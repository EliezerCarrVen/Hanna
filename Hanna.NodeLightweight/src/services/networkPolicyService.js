class NetworkPolicyService { status() { return { outbound_default: 'disabled_for_dangerous_actions', dry_run: true }; } }
module.exports = { NetworkPolicyService };
