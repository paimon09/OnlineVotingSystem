using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineVotingSystem.Models
{
    public class Candidate
    {
        public int CandidateId { get; set; }
        public string Name { get; set; }

        // nullable because some records may not have an election assigned
        public int? ElectionId { get; set; }

        // optional helper
        public string ElectionTitle { get; set; }

        // Active/Inactive status
        public string Status { get; set; }
    }
}