using System;
using System.Web.Mvc;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using OnlineVotingSystem.Helpers;
using OnlineVotingSystem.Models;

public class AdminController : Controller
{
    string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

    // Admin Login GET
    public ActionResult Login()
    {
        return View();
    }

    // Admin Login POST
    [HttpPost]
    public ActionResult Login(string username, string password)
    {
        string hash = HashHelper.GetSHA256(password);

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT AdminId FROM Admins WHERE Username = @Username AND PasswordHash = @Password", con);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", hash);

            object result = cmd.ExecuteScalar();
            if (result != null)
            {
                Session["AdminId"] = result;
                Session["AdminUser"] = username;
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
        if (Session["AdminId"] == null)
            return RedirectToAction("Login");

        return View();
    }

    public ActionResult AddElection()
    {
        return View();
    }

    [HttpPost]
    public ActionResult AddElection(string title, DateTime startDate, DateTime endDate)
    {
        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("INSERT INTO Elections (Title, StartDate, EndDate, Status) VALUES (@Title, @Start, @End, 'Active')", con);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Start", startDate);
            cmd.Parameters.AddWithValue("@End", endDate);
            cmd.ExecuteNonQuery();
        }

        ViewBag.Message = "Election created successfully!";
        return View();
    }

    // Toggle election status (Active <-> Inactive)
    public ActionResult ToggleElectionStatus(int id)
    {
        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            // get current status
            SqlCommand get = new SqlCommand("SELECT Status FROM Elections WHERE ElectionId = @Id", con);
            get.Parameters.AddWithValue("@Id", id);
            object s = get.ExecuteScalar();
            string newStatus = (s != null && s.ToString() == "Active") ? "Inactive" : "Active";

            SqlCommand upd = new SqlCommand("UPDATE Elections SET Status = @Status WHERE ElectionId = @Id", con);
            upd.Parameters.AddWithValue("@Status", newStatus);
            upd.Parameters.AddWithValue("@Id", id);
            upd.ExecuteNonQuery();
        }

        return RedirectToAction("Elections");
    }

    public ActionResult AddCandidate()
    {
        List<Election> elections = new List<Election>();

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT * FROM Elections WHERE Status = 'Active'", con);
            SqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                elections.Add(new Election
                {
                    ElectionId = Convert.ToInt32(rdr["ElectionId"]),
                    Title = rdr["Title"].ToString()
                });
            }
            rdr.Close();
        }

        ViewBag.Elections = new SelectList(elections, "ElectionId", "Title");
        return View();
    }

    [HttpPost]
    public ActionResult AddCandidate(string name, int electionId)
    {
        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            // Candidate.Status column expected in DB (default 'Active' if null)
            SqlCommand cmd = new SqlCommand("INSERT INTO Candidates (Name, ElectionId, Status) VALUES (@Name, @ElectionId, 'Active')", con);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@ElectionId", electionId);
            cmd.ExecuteNonQuery();
        }

        ViewBag.Candidate = "Candidate added successfully!";
        //ViewBag.Elections = new SelectList(elections, "ElectionId", "Title");
        return RedirectToAction("AddCandidate");
    }

    // Toggle candidate status
    public ActionResult ToggleCandidateStatus(int id)
    {
        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand get = new SqlCommand("SELECT Status FROM Candidates WHERE CandidateId = @Id", con);
            get.Parameters.AddWithValue("@Id", id);
            object s = get.ExecuteScalar();
            string newStatus = (s != null && s.ToString() == "Active") ? "Inactive" : "Active";

            SqlCommand upd = new SqlCommand("UPDATE Candidates SET Status = @Status WHERE CandidateId = @Id", con);
            upd.Parameters.AddWithValue("@Status", newStatus);
            upd.Parameters.AddWithValue("@Id", id);
            upd.ExecuteNonQuery();
        }

        return RedirectToAction("Candidates");
    }

    public ActionResult AddVoter()
    {
        // Populate elections dropdown (show active elections)
        List<Election> elections = new List<Election>();

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT ElectionId, Title FROM Elections WHERE Status = 'Active'", con);
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                elections.Add(new Election
                {
                    ElectionId = Convert.ToInt32(rdr["ElectionId"]),
                    Title = rdr["Title"].ToString()
                });
            }
            rdr.Close();
        }

        ViewBag.Elections = new SelectList(elections, "ElectionId", "Title");
        return View();
    }

    [HttpPost]
    public ActionResult AddVoter(string name, string email, string username, string password, int? electionId)
    {
        string hashed = HashHelper.GetSHA256(password);

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();

            // Save election association when creating voter (requires Voters.ElectionId column)
            // Also save IsActive = 1 by default (requires Voters.IsActive bit column)
            SqlCommand cmd = new SqlCommand("INSERT INTO Voters (Name, Email, Username, PasswordHash, HasVoted, ElectionId, IsActive) VALUES (@Name, @Email, @Username, @Password, 0, @ElectionId, 1)", con);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", hashed);
            if (electionId.HasValue)
                cmd.Parameters.AddWithValue("@ElectionId", electionId.Value);
            else
                cmd.Parameters.AddWithValue("@ElectionId", DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        ViewBag.Message = "Voter added successfully!";

        // Repopulate dropdown when redisplaying the form
        List<Election> elections = new List<Election>();
        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT ElectionId, Title FROM Elections WHERE Status = 'Active'", con);
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                elections.Add(new Election
                {
                    ElectionId = Convert.ToInt32(rdr["ElectionId"]),
                    Title = rdr["Title"].ToString()
                });
            }
            rdr.Close();
        }
        ViewBag.Elections = new SelectList(elections, "ElectionId", "Title", electionId);

        return View();
    }

    // Toggle voter active state
    public ActionResult ToggleVoterStatus(int id)
    {
        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand get = new SqlCommand("SELECT IsActive FROM Voters WHERE VoterId = @Id", con);
            get.Parameters.AddWithValue("@Id", id);
            object s = get.ExecuteScalar();
            bool current = (s != null && s != DBNull.Value && Convert.ToBoolean(s));
            bool newState = !current;

            SqlCommand upd = new SqlCommand("UPDATE Voters SET IsActive = @State WHERE VoterId = @Id", con);
            upd.Parameters.AddWithValue("@State", newState);
            upd.Parameters.AddWithValue("@Id", id);
            upd.ExecuteNonQuery();
        }

        return RedirectToAction("Voters");
    }

    public ActionResult Results()
    {
        // Get all elections
        List<Election> elections = new List<Election>();

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT ElectionId, Title FROM Elections", con);
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                elections.Add(new Election
                {
                    ElectionId = Convert.ToInt32(rdr["ElectionId"]),
                    Title = rdr["Title"].ToString()
                });
            }
            rdr.Close();
        }

        ViewBag.Elections = new SelectList(elections, "ElectionId", "Title");
        return View();
    }

    [HttpPost]
    public ActionResult Results(int electionId)
    {
        List<ResultViewModel> results = new List<ResultViewModel>();
        string electionName = "";

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();

            // Get election name
            SqlCommand electionCmd = new SqlCommand("SELECT Title FROM Elections WHERE ElectionId = @ElectionId", con);
            electionCmd.Parameters.AddWithValue("@ElectionId", electionId);
            electionName = electionCmd.ExecuteScalar()?.ToString();

            // Get results
            SqlCommand cmd = new SqlCommand(@"
            SELECT c.Name AS CandidateName, COUNT(v.VoteId) AS VoteCount
            FROM Votes v
            JOIN Candidates c ON v.CandidateId = c.CandidateId
            WHERE v.ElectionId = @ElectionId
            GROUP BY c.Name
            ORDER BY VoteCount DESC", con);
            cmd.Parameters.AddWithValue("@ElectionId", electionId);

            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                results.Add(new ResultViewModel
                {
                    CandidateName = rdr["CandidateName"].ToString(),
                    VoteCount = Convert.ToInt32(rdr["VoteCount"])
                });
            }
            rdr.Close();
        }

        ViewBag.ElectionTitle = electionName;
        ViewBag.ElectionId = electionId;
        ViewBag.Elections = new SelectList(GetAllElections(), "ElectionId", "Title", electionId);
        return View(results);
    }

    private List<Election> GetAllElections()
    {
        List<Election> elections = new List<Election>();

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT ElectionId, Title FROM Elections", con);
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                elections.Add(new Election
                {
                    ElectionId = Convert.ToInt32(rdr["ElectionId"]),
                    Title = rdr["Title"].ToString()
                });
            }
            rdr.Close();
        }

        return elections;
    }

    // GET: Admin/Elections
    public ActionResult Elections()
    {
        List<Election> elections = new List<Election>();

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("SELECT ElectionId, Title, StartDate, EndDate, Status FROM Elections", con);
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                elections.Add(new Election
                {
                    ElectionId = Convert.ToInt32(rdr["ElectionId"]),
                    Title = rdr["Title"].ToString(),
                    StartDate = rdr["StartDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["StartDate"]),
                    EndDate = rdr["EndDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["EndDate"]),
                    Status = rdr["Status"]?.ToString()
                });
            }
            rdr.Close();
        }

        return View(elections);
    }

    // alias for legacy URL /Admin/ViewElections -> redirects to /Admin/Elections
    public ActionResult ViewElections()
    {
        return RedirectToAction("Elections");
    }

    // GET: Admin/Candidates
    public ActionResult Candidates()
    {
        List<Candidate> candidates = new List<Candidate>();
        var electionMap = new Dictionary<int, string>();

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();

            // load election titles (map)
            SqlCommand eCmd = new SqlCommand("SELECT ElectionId, Title FROM Elections", con);
            SqlDataReader eRdr = eCmd.ExecuteReader();
            while (eRdr.Read())
            {
                electionMap[Convert.ToInt32(eRdr["ElectionId"])] = eRdr["Title"].ToString();
            }
            eRdr.Close();

            // load candidates (include Status)
            SqlCommand cCmd = new SqlCommand("SELECT CandidateId, Name, ElectionId, Status FROM Candidates", con);
            SqlDataReader cRdr = cCmd.ExecuteReader();
            while (cRdr.Read())
            {
                candidates.Add(new Candidate
                {
                    CandidateId = Convert.ToInt32(cRdr["CandidateId"]),
                    Name = cRdr["Name"].ToString(),
                    ElectionId = cRdr["ElectionId"] == DBNull.Value ? (int?)null : Convert.ToInt32(cRdr["ElectionId"]),
                    Status = cRdr["Status"] == DBNull.Value ? "Active" : cRdr["Status"].ToString()
                });
            }
            cRdr.Close();
        }

        ViewBag.ElectionMap = electionMap;
        return View(candidates);
    }

    // GET: Admin/Voters
    public ActionResult Voters()
    {
        List<Voter> voters = new List<Voter>();
        var electionMap = new Dictionary<int, string>();

        using (SqlConnection con = new SqlConnection(connStr))
        {
            con.Open();

            // load election titles (map)
            SqlCommand eCmd = new SqlCommand("SELECT ElectionId, Title FROM Elections", con);
            SqlDataReader eRdr = eCmd.ExecuteReader();
            while (eRdr.Read())
            {
                electionMap[Convert.ToInt32(eRdr["ElectionId"])] = eRdr["Title"].ToString();
            }
            eRdr.Close();

            // load voters (include IsActive)
            SqlCommand vCmd = new SqlCommand("SELECT VoterId, Name, Email, Username, HasVoted, ElectionId, IsActive FROM Voters", con);
            SqlDataReader vRdr = vCmd.ExecuteReader();
            while (vRdr.Read())
            {
                voters.Add(new Voter
                {
                    VoterId = Convert.ToInt32(vRdr["VoterId"]),
                    Name = vRdr["Name"].ToString(),
                    Email = vRdr["Email"]?.ToString(),
                    Username = vRdr["Username"]?.ToString(),
                    HasVoted = vRdr["HasVoted"] != DBNull.Value && Convert.ToBoolean(vRdr["HasVoted"]),
                    ElectionId = vRdr["ElectionId"] == DBNull.Value ? (int?)null : Convert.ToInt32(vRdr["ElectionId"]),
                    IsActive = vRdr["IsActive"] != DBNull.Value && Convert.ToBoolean(vRdr["IsActive"])
                });
            }
            vRdr.Close();
        }

        ViewBag.ElectionMap = electionMap;
        return View(voters);
    }
}
