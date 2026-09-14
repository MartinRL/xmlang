using AwesomeAssertions;
using Emlang;
using Xunit;

namespace Xmlang.Tests;

/// <summary>The v0.6 rules (RFC xmlang-0001): confirm, then, labels with arguments and enum
/// values, during per decision model, and the cross-checks against initiators and scenarios.
/// One smallest-failing document per rule against a two-decider LOB model.</summary>
public class XmLinterV06Tests
{
    private const string Em = """
        slices:
          ✍️ Draft invoice:
            steps:
              - a: 🧾 Clerk /Invoice list
              - c: DraftInvoice
                props: { supplierId: Guid, invoiceNumber: string }
              - x: SupplierInactive
              - e: Invoice / InvoiceDrafted
                props: { invoiceId: Guid, supplierId: Guid, createdBy: Guid }
              - v: Invoice details
                props: { invoiceId: Guid, invoiceNumber: string, lineCount: int, phase: InvoicePhase (draft|open|paid), lines: "InvoiceLine[]" }
            tests:
              drafts:
                given:
                  - s: Invoice
                when:
                  - c: DraftInvoice
                then:
                  - e: Invoice / InvoiceDrafted
          ✍️ Approve invoice:
            steps:
              - a: ✅ Approver /Approval queue
              - c: ApproveInvoice
                props: { invoiceId: Guid }
              - x: NotSubmitted
              - e: Invoice / InvoiceApproved
                props: { invoiceId: Guid, approvedBy: Guid }
              - v: Approval queue
                props: { pending: "PendingInvoice[]" }
            tests:
              approves an open invoice:
                given:
                  - s: Invoice
                    props: { phase: open }
                when:
                  - c: ApproveInvoice
                then:
                  - e: Invoice / InvoiceApproved
              cannot approve a draft:
                given:
                  - s: Invoice
                    props: { phase: draft }
                when:
                  - c: ApproveInvoice
                then:
                  - x: NotSubmitted
          ⚙️ Pay:
            steps:
              - auto: ⚙️ System /Due payments
              - c: PayInvoice
              - e: Invoice / InvoicePaid
                props: { invoiceId: Guid }
          👀 Decision models:
            steps:
              - s: Invoice
                props: { invoiceId: Guid, phase: InvoicePhase (draft|open|paid) }
              - s: Supplier
                props: { supplierId: Guid, phase: SupplierPhase (open|closed) }
        """;

    private static readonly EmSpec ParsedEm = EmParser.Parse(Em);

    private const string Header = """
        xmlang: "0.6"
        personas:
          Clerk:
            role: 🧾 Clerk
          Approver:
            role: approver
        """;

    private static IReadOnlyList<XmFinding> Lint(string body) =>
        XmLinter.Lint(XmParser.Parse(Header + "\n" + body), ParsedEm);

    private static IReadOnlyList<string> Rules(string body) => [.. Lint(body).Select(f => f.Rule)];

    [Fact]
    public void Normalized_roles_match_emoji_prefixed_initiators() =>
        Lint("""
            surfaces:
              ClerkHome:
                for: [Clerk]
                compose:
                  - v: Invoice details
                  - c: DraftInvoice
            """).Should().BeEmpty();

    [Fact]
    public void Confirm_must_be_boolean_and_habituation_warns()
    {
        Rules("""
            surfaces:
              Approvals:
                compose:
                  - v: Approval queue
                  - c: ApproveInvoice
                    confirm: maybe
            """).Should().Contain("xm-confirm-not-boolean");
        Rules("""
            surfaces:
              Approvals:
                compose:
                  - v: Approval queue
                  - c: ApproveInvoice
                    confirm: true
                  - c: DraftInvoice
                    confirm: true
                  - c: PayInvoice
            """).Should().Contain("xm-confirm-habituation");
    }

    [Fact]
    public void Then_resolves_to_a_surface_for_every_persona_and_flags_the_default()
    {
        Rules("""
            surfaces:
              Approvals:
                compose:
                  - v: Approval queue
                  - c: ApproveInvoice
                    then: Nowhere
            """).Should().Contain("xm-dangling-ref");
        Rules("""
            surfaces:
              Approvals:
                for: [Approver]
                compose:
                  - v: Invoice details
                  - c: ApproveInvoice
                    then: ClerkHome
              ClerkHome:
                for: [Clerk]
                compose:
                  - v: Invoice details
            """).Should().Contain("xm-then-persona-mismatch");
        Rules("""
            surfaces:
              Record:
                compose:
                  - v: Invoice details
                  - c: ApproveInvoice
                    then: Queue
              Queue:
                compose:
                  - v: Approval queue
            """).Should().Contain("xm-then-restates-default");
    }

    [Fact]
    public void During_resolves_per_decision_model()
    {
        Rules("surfaces:\n  S:\n    during: [open]\n    compose: [{ v: Invoice details }]\n")
            .Should().Contain("xm-ambiguous-phase");
        Rules("surfaces:\n  S:\n    during: { Invoice: [open] }\n    compose: [{ v: Invoice details }]\n")
            .Should().NotContain(r => r.StartsWith("xm-ambiguous") || r == "xm-unknown-phase");
        Rules("surfaces:\n  S:\n    during: { Invoice: [nope] }\n    compose: [{ v: Invoice details }]\n")
            .Should().Contain("xm-unknown-phase");
        Rules("surfaces:\n  S:\n    during: { Ghost: [open] }\n    compose: [{ v: Invoice details }]\n")
            .Should().Contain("xm-unknown-phase");
        Lint("surfaces:\n  S:\n    during: { Invoice: [draft] }\n    compose: [{ v: Invoice details }]\n")
            .Where(f => f.Rule == "xm-phase-uncovered").Select(f => f.Message)
            .Should().BeEquivalentTo(
                "phase 'open' of 'Invoice' is claimed by no surface",
                "phase 'paid' of 'Invoice' is claimed by no surface",
                "phase 'open' of 'Supplier' is claimed by no surface",
                "phase 'closed' of 'Supplier' is claimed by no surface");
    }

    [Fact]
    public void A_command_on_a_persona_surface_needs_an_initiator_with_that_role()
    {
        Rules("""
            surfaces:
              ClerkHome:
                for: [Clerk]
                compose:
                  - v: Approval queue
                  - c: ApproveInvoice
            """).Should().Contain("xm-command-trigger-mismatch");
        Rules("""
            surfaces:
              Approvals:
                for: [Approver]
                compose:
                  - v: Approval queue
                  - c: ApproveInvoice
            """).Should().NotContain("xm-command-trigger-mismatch");
    }

    [Fact]
    public void A_command_offered_in_a_phase_without_a_success_scenario_is_info()
    {
        Rules("surfaces:\n  S:\n    during: { Invoice: [draft] }\n    compose: [{ v: Approval queue }, { c: ApproveInvoice }]\n")
            .Should().Contain("xm-command-phase-mismatch");
        Rules("surfaces:\n  S:\n    during: { Invoice: [open] }\n    compose: [{ v: Approval queue }, { c: ApproveInvoice }]\n")
            .Should().NotContain("xm-command-phase-mismatch");
    }

    [Fact]
    public void Two_view_only_surfaces_in_one_cell_composing_the_same_view_are_ambiguous() =>
        Rules("""
            surfaces:
              A:
                during: { Invoice: [open] }
                compose: [{ v: Invoice details }]
              B:
                during: { Invoice: [open, paid] }
                compose: [{ v: Invoice details }]
            """).Should().Contain("xm-cell-ambiguous");

    [Fact]
    public void An_initiator_origin_naming_a_surface_must_be_admitted_there()
    {
        Rules("""
            surfaces:
              Invoice list:
                for: [Approver]
                compose: [{ v: Invoice details }]
            """).Should().Contain("xm-origin-mismatch");
        Rules("""
            surfaces:
              Invoice list:
                for: [Clerk]
                compose: [{ v: Invoice details }]
            """).Should().NotContain("xm-origin-mismatch");
    }

    [Fact]
    public void Labels_take_exceptions_confirm_questions_enum_values_and_capped_icu_arguments()
    {
        Lint("""
            labels:
              en:
                SupplierInactive: This supplier is inactive.
                ApproveInvoice:
                  $self: Approve
                  $confirm: "Approve {invoiceId}?"
                Invoice details:
                  $self: "Invoice {invoiceNumber}"
                  lineCount: "{lineCount, plural, one {# line} other {# lines}}"
                  phase:
                    $self: Status
                    $values: { draft: Draft, open: Awaiting approval, paid: Paid }
            """).Should().BeEmpty();

        Rules("""
            labels:
              en:
                SupplierInactive:
                  $self: This supplier is inactive.
                Invoice details:
                  $self: "Invoice {nope} {lines}"
                  phase:
                    $values: { bogus: X }
                  invoiceNumber: "{invoiceNumber, select, a {A} other {B}}"
            """).Should().BeEquivalentTo(
                "xm-orphan-label", "xm-label-arg-missing", "xm-label-arg-missing", "xm-orphan-label", "xm-label-grammar");
    }

    [Fact]
    public void A_surface_label_argument_resolves_against_its_composed_views_unambiguously()
    {
        Rules("""
            surfaces:
              Record:
                compose: [{ v: Invoice details }, { v: Approval queue }]
            labels:
              en:
                Record: "Invoice {invoiceNumber}"
            """).Should().NotContain("xm-label-arg-missing");
        Rules("""
            surfaces:
              Record:
                compose: [{ v: Invoice details }]
            labels:
              en:
                Record: "{pending}"
            """).Should().Contain("xm-label-arg-missing");
    }
}
