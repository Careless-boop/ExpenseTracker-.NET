namespace ExpenseTracker.Application.Features.ExpenseLists.Transactions.Commands
{
    internal static class ParticipantSplitRules
    {
        public const string Message =
            "Custom participant shares must sum to the transaction amount when every participant has " +
            "one, and must not exceed it otherwise.";

        /// <summary>
        /// Custom shares have to reconcile with the total. Otherwise balances silently stop summing
        /// to zero — the payer is credited the full Amount while the participants are only debited
        /// their custom sum, and the debt simplifier quietly swallows the difference.
        /// </summary>
        public static bool SharesReconcile(IReadOnlyList<ParticipantInput>? participants, decimal amount)
        {
            if (participants is null || participants.Count == 0)
                return true;

            var customShares = participants
                .Where(p => p.CustomShareAmount.HasValue)
                .Select(p => p.CustomShareAmount!.Value)
                .ToList();

            if (customShares.Count == 0)
                return true;

            var customTotal = customShares.Sum();

            // Every participant is custom: between them they must account for the whole amount.
            if (customShares.Count == participants.Count)
                return customTotal == amount;

            // Mixed: whatever is left over gets split equally among the rest, so there has to be
            // something left over.
            return customTotal < amount;
        }
    }
}
