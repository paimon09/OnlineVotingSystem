# Online Voting System

## 📌 Project Overview

The Online Voting System is a web-based application developed using ASP.NET MVC that enables secure and efficient digital voting. The system allows administrators to manage elections, candidates, and voters, while registered voters can cast their votes online. It ensures transparency, accuracy, and fair election management through automated vote counting and result generation.

## 🚀 Features

* Admin and Voter Authentication
* Election Management
* Candidate Management
* Voter Registration and Management
* Secure Vote Casting
* One Vote Per Voter
* Real-Time Vote Counting
* Election Result Dashboard
* User-Friendly Interface

## 🛠️ Technologies Used

### Frontend

* HTML5
* CSS3
* JavaScript

### Backend

* C#
* ASP.NET MVC

### Database

* Microsoft SQL Server (MSSQL)

### Development Tools

* Visual Studio
* IIS Express

## 📂 Project Structure

```text
OnlineVotingSystem/
│
├── Controllers/
│   ├── AdminController.cs
│   ├── HomeController.cs
│   └── VoterController.cs
│
├── Models/
│   ├── Admin.cs
│   ├── Candidate.cs
│   ├── Elections.cs
│   └── Voter.cs
│
├── Views/
│   ├── Admin/
│   ├── Home/
│   └── Shared/
│
├── Content/
├── Scripts/
├── App_Data/
└── OnlineVotingDB.mdf
```

## ⚙️ Installation

1. Clone the repository:

```bash
git clone https://github.com/your-username/OnlineVotingSystem.git
```

2. Open the solution in Visual Studio.

3. Restore NuGet packages if required.

4. Configure the SQL Server connection string.

5. Build and run the project using IIS Express.

## 📖 Usage

1. Log in as Admin or Voter.
2. Admin creates elections and manages candidates.
3. Register voters in the system.
4. Voters cast their votes during active elections.
5. View election results after voting is completed.

## 🎯 Learning Outcomes

* ASP.NET MVC Architecture
* C# Programming
* Database Design with SQL Server
* Authentication and Authorization
* CRUD Operations
* Frontend Development
* Software Development Lifecycle

## 🔮 Future Enhancements

* OTP/Email Verification
* Password Encryption
* Audit Logs
* Responsive Mobile Design
* Advanced Analytics Dashboard
* Multi-Factor Authentication
