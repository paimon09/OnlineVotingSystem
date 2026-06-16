using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineVotingSystem.Models
{
    public class Election
    {
        public int ElectionId { get; set; }
        public string Title { get; set; }

        // Make StartDate/EndDate nullable because views/controllers check HasValue
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; }
    }

}