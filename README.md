# AI Support Assist API

**Live Demo (Azure)**

https://aisupportassistapi-avfmddadh3hjgea9.southeastasia-01.azurewebsites.net/swagger

Deployed using Azure Database and Azure App Service.

## Overview

AI Support Assist API is a backend-only ASP.NET Core Web API that automates repetitive customer support queries using Retrieval-Augmented Generation (RAG), SQL Server, and Large Language Model (LLM) integration.

Small and medium-sized businesses frequently receive repetitive customer support questions such as delivery timelines, refund policies, order tracking, and payment issues. Handling these manually increases operational cost and slows response time.

This project demonstrates how a backend API can:

Retrieve trusted business FAQs from a SQL Server database

Ground AI responses using FAQs

Generate intelligent answers using an external LLM

Flag uncertain responses for human escalation

## Architecture

Client (Swagger / Postman)

→ ASP.NET Core Web API

→ Controller

→ Service Layer (RAG Logic)

→ EF Core

→ SQL Server (FAQ Knowledge Base)

→ Secured using JWT

→ External LLM API (Groq)

→ Grounded Response with Confidence Flag

The project follows a strict Controller → Service → DbContext architecture without unnecessary repository.

## Key Features
### 1. Clean Backend Architecture

Clear separation of concerns

Dependency Injection throughout


### 2. Retrieval-Augmented Generation (RAG)

Retrieves relevant FAQs from SQL Server

Injects only context-specific knowledge into the prompt

Constrains AI responses to trusted business data

Reduces hallucination risk

### 3. Context-Specific Retrieval

Keyword-based relevance scoring

Top-matching FAQs injected into the prompt

Reduced token usage and improved response accuracy

### 4. Confidence Evaluation

Detects uncertain or incomplete responses

Flags responses for potential human review

Improves system reliability

### 5. Secure Configuration

API keys stored in Azure App Settings

Environment-variable ready

No hardcoded secrets

## Technology Stack

ASP.NET Core Web API

Entity Framework Core

SQL Server

Groq LLM API

Swagger (OpenAPI)

C#

## Project Structure
`AI-Support-Assist-API`
│

├── `Controllers`

│   ├── SupportController.cs

│   ├── AuthController.cs

│   └── AdminController.cs
│

├── `Services`

│   ├── SupportService.cs

│   ├── GroqService.cs

│   ├── AuthService.cs

│   └── FaqService.cs
│

├── `Data`

│   ├── AppDbContext.cs

│   └── IdentitySeeder.cs
│

├── `Models`

│   ├── Entities

│   └── DTOs
│

└── `Program.cs`

## How RAG Is Implemented

This project implements a practical, lightweight RAG pipeline:

Retrieve relevant FAQs from SQL Server using keyword-based scoring.

Select top matching FAQs based on relevance.

Inject the selected FAQs into the LLM prompt.

Generate a grounded response.

Apply confidence evaluation logic to determine whether escalation is required.

This approach avoids unnecessary embedding infrastructure while remaining scalable for future upgrades.

## Sample Endpoint
#### POST `/api/support/ask`

#### Request:

{
  "question": "What is your refund policy?"
}


#### Response:

{
  "answer": "Refunds are processed within 7 business days after approval.",
  "requiresHumanReview": false
  "confidenceScore": 1
}

## Database Design

SQL Server as primary data store

FAQ entity used as the knowledge base

EF Core for data access

Seeded reference data for deterministic testing

The system is read-heavy and optimized for fast retrieval.

## Running the Project Locally

Clone the repository

Update appsettings.json:

SQL Server connection string

LLM API key

Apply migrations:

`Update-Database`

Run the API

Open Swagger at:

https://localhost:<port>/swagger


## Author

Kamalesh
C# / .NET Backend Developer
Focused on AI-integrated backend systems and scalable architecture

