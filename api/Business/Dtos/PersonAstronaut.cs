namespace StargateAPI.Business.Dtos
{
    public class PersonAstronaut
    {
        public int PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        // Seems like CurrentRank/CurrentDutyTitle should be nullable/null if person is not an astronaut, makes schema
        // more consistent with nullable DateTimes
        public string? CurrentRank { get; set; }

        public string? CurrentDutyTitle { get; set; }

        public DateTime? CareerStartDate { get; set; }

        public DateTime? CareerEndDate { get; set; }


    }
}
