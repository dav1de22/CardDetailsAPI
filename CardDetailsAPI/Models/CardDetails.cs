
    namespace CardDetailsAPI.Models
    {
        public class CardDetails
        {
            public string Number { get; set; }
            public Dictionary<int, string> People { get; set; }
            public string Expiry { get; set; }


            public string ImageFile { get; set; }
            public CardDetails()
            {
                People = new Dictionary<int, string>();
            }

        }
    }