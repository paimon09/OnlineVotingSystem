using System.Web.Mvc;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System;
using OnlineVotingSystem.Helpers;
using OnlineVotingSystem.Models;
using System.Collections.Generic;

public class VoterController : Controller
{
    string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

    public ActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Login(string username, string password)
    {
        string hash = HashHelper.GetSHA256(password);

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
           
            SqlCommand cmd = new SqlCommand("SELECT VoterId, Name, ElectionId, IsActive FROM Voters WHERE Username = @Username AND PasswordHash = @Password", con);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", hash);

            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                bool isActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]);
                if (!isActive)
                {
                    ViewBag.Message = "Your account is deactivated. Contact the administrator.";
                    return View();
                }

                Session["VoterId"] = reader["VoterId"];
                Session["VoterName"] = reader["Name"];

                if (reader["ElectionId"] != DBNull.Value)
                    Session["VoterElectionId"] = Convert.ToInt32(reader["ElectionId"]);
                else
                    Session["VoterElectionId"] = null;

                reader.Close();

                if (Session["VoterElectionId"] != null)
                {
                    int eid = Convert.ToInt32(Session["VoterElectionId"]);
                    SqlCommand eCmd = new SqlCommand("SELECT Status FROM Elections WHERE ElectionId = @ElectionId", con);
                    eCmd.Parameters.AddWithValue("@ElectionId", eid);
                    object s = eCmd.ExecuteScalar();
                    string status = s?.ToString();
                    if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                       
                        Session.Clear();
                        ViewBag.Message = "Your assigned election is not active.";
                        return View();
                    }
                }

                return RedirectToAction("Dashboard");
            }
        }

        ViewBag.Message = "Invalid credentials";
        return View();
    }

    public ActionResult Logout()
    {
        Session.Clear();
        return RedirectToAction("Login");
    }

    public ActionResult Dashboard()
    {
        if (Session["VoterId"] == null)
            return RedirectToAction("Login");

        List<Election> elections = new List<Election>();

        int? assignedElectionId = null;
        if (Session["VoterElectionId"] != null && int.TryParse(Session["VoterElectionId"].ToString(), out int tmp))
            assignedElectionId = tmp;

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();

            if (assignedElectionId.HasValue)
            {
                SqlCommand cmd = new SqlCommand("SELECT ElectionId, Title FROM Elections WHERE Status = 'Active' AND ElectionId = @ElectionId", con);
                cmd.Parameters.AddWithValue("@ElectionId", assignedElectionId.Value);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    elections.Add(new Election
                    {
                        ElectionId = Convert.ToInt32(reader["ElectionId"]),
                        Title = reader["Title"].ToString()
                    });
                }
                reader.Close();
            }
           
        }

        return View(elections);
    }

    public ActionResult Vote(int electionId)
    {
        if (Session["VoterId"] == null)
            return RedirectToAction("Login");

        int voterId = Convert.ToInt32(Session["VoterId"]);

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();

           
            SqlCommand chk = new SqlCommand("SELECT HasVoted FROM Voters WHERE VoterId = @VoterId", con);
            chk.Parameters.AddWithValue("@VoterId", voterId);
            object hv = chk.ExecuteScalar();
            if (hv != null && Convert.ToBoolean(hv))
            {
               
                return RedirectToAction("ThankYou", "Voter");
            }

            if (Session["VoterElectionId"] != null)
            {
                int assigned = Convert.ToInt32(Session["VoterElectionId"]);
                if (assigned != electionId)
                {
                    TempData["Message"] = "You are not assigned to this election.";
                    return RedirectToAction("Dashboard");
                }
            }

            List<Candidate> candidates = new List<Candidate>();

    
            SqlCommand cmd = new SqlCommand("SELECT CandidateId, Name FROM Candidates WHERE ElectionId = @ElectionId AND (Status IS NULL OR Status = 'Active')", con);
            cmd.Parameters.AddWithValue("@ElectionId", electionId);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                candidates.Add(new Candidate
                {
                    CandidateId = Convert.ToInt32(reader["CandidateId"]),
                    Name = reader["Name"].ToString()
                });
            }
            reader.Close();

            ViewBag.ElectionId = electionId;
            return View(candidates);
        }
    }

    [HttpPost]
    public ActionResult Vote(int candidateId, int electionId)
    {
        if (Session["VoterId"] == null)
            return RedirectToAction("Login");

        int voterId = Convert.ToInt32(Session["VoterId"]);

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();

            SqlCommand chk = new SqlCommand("SELECT HasVoted FROM Voters WHERE VoterId = @VoterId", con);
            chk.Parameters.AddWithValue("@VoterId", voterId);
            object hv = chk.ExecuteScalar();
            if (hv != null && Convert.ToBoolean(hv))
            {
               
                return RedirectToAction("ThankYou", "Voter");
            }

         
            SqlCommand cmd = new SqlCommand("INSERT INTO Votes (VoterId, CandidateId, ElectionId) VALUES (@VoterId, @CandidateId, @ElectionId)", con);
            cmd.Parameters.AddWithValue("@VoterId", voterId);
            cmd.Parameters.AddWithValue("@CandidateId", candidateId);
            cmd.Parameters.AddWithValue("@ElectionId", electionId);
            cmd.ExecuteNonQuery();

     
            SqlCommand up = new SqlCommand("UPDATE Voters SET HasVoted = 1 WHERE VoterId = @VoterId", con);
            up.Parameters.AddWithValue("@VoterId", voterId);
            up.ExecuteNonQuery();
        }

        return RedirectToAction("ThankYou", "Voter");
    }

    public ActionResult ThankYou()
    {
        return View();
    }
}
