namespace DataObjects
{
    public class MatchRequest
    {
        public int RequestId { get; set; }
        public Player? Player { get; set; }
        public double MatchFee { get; set; } // maybe turn this to MatchType if the decision will be made to group player according to the type of match and not just the money
        public Currencies? MatchFeeCurrency { get; set; }
    }
}
