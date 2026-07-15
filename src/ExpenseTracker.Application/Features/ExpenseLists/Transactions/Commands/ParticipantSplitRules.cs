namespace ExpenseTracker.Application.Features.ExpenseLists.Transactions.Commands
{
    internal static class ParticipantSplitRules
    {
        public const string Message =
            "Custom participant shares must sum to the transaction amount when every participant has " +
            "one, and must not exceed it otherwise.";

        public const string SplitRemainderMessage =
            "Split the rest only applies when every participant has a custom share, and those shares " +
            "must not exceed the transaction amount.";

        /// <summary>
        /// Custom shares have to reconcile with the total. Otherwise balances silently stop summing
        /// to zero — the payer is credited the full Amount while the participants are only debited
        /// their custom sum, and the debt simplifier quietly swallows the difference.
        /// </summary>
        public static bool SharesReconcile(
            IReadOnlyList<ParticipantInput>? participants,
            decimal amount,
            bool splitRemainder = false)
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

            // Every participant is custom: between them they must account for the whole amount,
            // unless the caller opted into spreading the shortfall back over them.
            if (customShares.Count == participants.Count)
                return splitRemainder ? customTotal <= amount : customTotal == amount;

            // Mixed: whatever is left over gets split equally among the rest, so there has to be
            // something left over.
            return customTotal < amount;
        }

        /// <summary>
        /// The flag is a no-op unless every participant is custom — with any equal-share participant
        /// present the remainder already goes to them. Rejecting it keeps the stored intent honest.
        /// </summary>
        public static bool SplitRemainderIsApplicable(
            IReadOnlyList<ParticipantInput>? participants,
            bool splitRemainder)
        {
            if (!splitRemainder)
                return true;

            return participants is { Count: > 0 } &&
                   participants.All(p => p.CustomShareAmount.HasValue);
        }
    }
}
