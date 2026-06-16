using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OnlineVotingSystem.Models
{
    public class Voter
    {
        public int VoterId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool HasVoted { get; internal set; }

        // Election association: nullable because older records may not have an election
        public int? ElectionId { get; set; }

        // New: whether voter account is active (controls login)
        public bool IsActive { get; set; }
    }

}