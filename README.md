# 🎯 SkillMatch

> A skill-based recruitment and candidate matching platform that helps recruiters find suitable candidates and enables candidates to understand their compatibility with job opportunities.

SkillMatch is a backend API designed to simplify the recruitment process by comparing candidate skills and experience against job requirements. It also supports resume uploading and automatic text extraction from PDF and DOCX files.

---

## 🚀 Features

### 👨‍💻 Candidate Features

* Create and manage candidate profiles
* Add and manage candidate skills
* Upload resumes in **PDF** and **DOCX** formats
* Automatically extract text from uploaded resumes
* View job compatibility
* Get detailed skill-gap analysis
* Identify matched and missing skills
* Compare candidate experience against job requirements

### 🏢 Recruiter Features

* Create and manage job postings
* Define required skills for jobs
* Specify minimum experience requirements
* View candidate information
* Evaluate candidates based on skills and experience

### 🧠 Matching Engine

SkillMatch uses a dedicated matching engine to evaluate candidates against job requirements.

The matching system considers:

* Skill compatibility
* Required vs. available skills
* Candidate experience
* Job experience requirements
* Overall candidate-job compatibility

The API provides a breakdown containing:

* Overall match score
* Skill match score
* Experience fit score
* Matched skills
* Missing skills
* Explanation of the result

---

## 📄 Resume Processing

Candidates can upload resumes directly through the API.

Supported formats:

* `.pdf`
* `.docx`

The system:

1. Receives the uploaded resume
2. Validates the file type
3. Stores the file
4. Extracts its text
5. Saves the parsed content
6. Returns a preview of the extracted text

PDF processing is handled using **PdfPig**, while DOCX processing uses **DocumentFormat.OpenXml**.

---

## 🛠️ Tech Stack

### Backend

* **C#**
* **ASP.NET Core 9**
* **.NET 9**
* **Entity Framework Core 9**
* **MySQL**
* **REST API**

### Document Processing

* **PdfPig**
* **DocumentFormat.OpenXml**

### API Documentation

* **Swagger / OpenAPI**

### Architecture

```text
SkillMatch
│
├── SkillMatch.API
│   │
│   ├── Controllers
│   │   ├── AdminController.cs
│   │   ├── CandidateController.cs
│   │   └── RecruiterController.cs
│   │
│   ├── Models
│   │
│   ├── Services
│   │   ├── MatchingEngine.cs
│   │   └── ResumeParserService.cs
│   │
│   ├── Properties
│   │
│   ├── wwwroot
│   │   └── uploads
│   │
│   ├── Program.cs
│   ├── appsettings.json
│   └── SkillMatch.API.csproj
│
└── SkillMatch.slnx
```

The repository currently follows this controller/service structure.

---

## ⚙️ Requirements

Before running the project, make sure you have:

* [.NET 9 SDK](https://dotnet.microsoft.com/)
* MySQL Server
* Git

---

## 📥 Installation

### 1. Clone the repository

```bash
git clone https://github.com/Rishisingh10/SkillMatch.git
```

### 2. Navigate to the project

```bash
cd SkillMatch
```

### 3. Restore dependencies

```bash
dotnet restore
```

### 4. Configure the database

Update the database connection string in:

```text
SkillMatch.API/appsettings.json
```

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=SkillMatch;user=root;password=YOUR_PASSWORD"
  }
}
```

Replace `YOUR_PASSWORD` with your MySQL password.

---

## 🗄️ Database Setup

Create the database in MySQL:

```sql
CREATE DATABASE SkillMatch;
```

Then apply the Entity Framework migrations:

```bash
dotnet ef database update
```

If the EF CLI is not installed:

```bash
dotnet tool install --global dotnet-ef
```

---

## ▶️ Running the Application

Navigate to the API directory:

```bash
cd SkillMatch.API
```

Run the application:

```bash
dotnet run
```

The API will start using the configured ASP.NET Core environment.

Swagger can then be used to explore and test the available API endpoints.

---

## 📚 API Structure

The API follows the standard ASP.NET Core controller structure:

```text
/api/Candidate
/api/Recruiter
/api/Admin
```

### Candidate API

Candidate functionality includes resume uploads and candidate-job analysis.

Example:

```http
GET /api/Candidate/{candidateId}/gap-analysis/{jobId}
```

The gap-analysis endpoint evaluates the candidate's skills and experience against a specific job and returns match scores, matched skills, missing skills, and an explanation.

### Resume Upload

```http
POST /api/Candidate/{candidateId}/resume/upload
```

The endpoint accepts multipart form data and supports:

```text
.pdf
.docx
```

---

## 🧩 Matching Workflow

```text
                 ┌──────────────────┐
                 │ Candidate Profile│
                 └────────┬─────────┘
                          │
                          ▼
                  ┌───────────────┐
                  │ Candidate     │
                  │ Skills        │
                  └───────┬───────┘
                          │
                          │
                          ▼
                   ┌─────────────┐
                   │   Matching   │
                   │    Engine    │
                   └──────┬──────┘
                          ▲
                          │
                          │
                  ┌───────┴────────┐
                  │ Job Requirements│
                  └───────┬────────┘
                          │
                          ▼
                  ┌───────────────┐
                  │ Match Analysis│
                  └───────┬───────┘
                          │
             ┌────────────┼────────────┐
             ▼            ▼            ▼
       Skill Score   Experience    Overall
                        Score        Score
```

---

## 📊 Match Analysis

A candidate-job evaluation returns information such as:

```json
{
  "jobId": 1,
  "jobTitle": "Software Engineer",
  "matchSummary": {
    "overallScore": 85,
    "skillMatchScore": 90,
    "experienceFitScore": 80
  },
  "skillBreakdown": {
    "matchedSkills": [
      "C#",
      ".NET",
      "SQL"
    ],
    "missingSkills": [
      "Docker"
    ]
  },
  "explanation": "Candidate has strong technical skill compatibility..."
}
```

This makes the matching result more useful than a simple yes/no recommendation.

---

## 🔄 Application Flow

### Candidate

```text
Register
   ↓
Create Profile
   ↓
Add Skills
   ↓
Upload Resume
   ↓
Resume Text Extraction
   ↓
View Available Jobs
   ↓
Run Skill Match
   ↓
View Skill Gap Analysis
```

### Recruiter

```text
Create Recruiter Profile
        ↓
Create Job
        ↓
Define Required Skills
        ↓
Set Experience Requirements
        ↓
Evaluate Candidates
        ↓
Review Match Scores
```

---

## 🔐 Security Considerations

For production deployment, configure sensitive values using environment variables or a secure secrets manager instead of committing credentials to `appsettings.json`.

Recommended production practices:

* Use HTTPS
* Protect database credentials
* Validate uploaded files
* Restrict upload sizes
* Implement authentication and authorization
* Add rate limiting
* Store uploaded resumes securely
* Avoid exposing sensitive candidate information

---

## 📌 Project Status

🚧 **Active Development**

The current repository contains the core API structure, candidate and recruiter controllers, resume processing services, matching logic, database integration, and API documentation setup.

Future improvements can include:

* JWT authentication
* Role-based authorization
* Advanced semantic skill matching
* AI-powered resume analysis
* Candidate recommendation ranking
* Recruiter dashboard
* Candidate dashboard
* Job recommendation system
* Skill-gap learning recommendations
* Docker deployment
* Cloud deployment
* Automated testing
* CI/CD pipeline

---

## 🤝 Contributing

Contributions are welcome.

1. Fork the repository
2. Create a feature branch

```bash
git checkout -b feature/your-feature
```

3. Commit your changes

```bash
git commit -m "Add your feature"
```

4. Push the branch

```bash
git push origin feature/your-feature
```

5. Open a Pull Request

---

## 📄 License

This project currently does not specify a license.

If you intend to make the project open source, consider adding an appropriate license such as MIT.

---

## 👨‍💻 Author

**Rishisingh10**

GitHub:
https://github.com/Rishisingh10

---

## ⭐ Support

If you find this project useful, consider giving the repository a ⭐ on GitHub.

**Repository:**
https://github.com/Rishisingh10/SkillMatch
