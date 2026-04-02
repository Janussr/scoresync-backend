# ScoreSync – Backend

This is a hobby project built to support game nights with friends.

The goal is to replace manual score tracking (e.g. Excel) with a simple system that can track scores, determine winners, and store game history over time.

## Features

- Create and manage games
- Track player scores across rounds
- Calculate winners
- Store historical results
- Real-time updates using SignalR

## Tech Stack

- .NET 10
- Entity Framework Core (Code First)
- MSSQL
- SignalR
- xUnit (v3)

## Architecture

The project is structured around a service-based approach with clear separation between:
- Controllers (API layer)
- Services (business logic)
- Data layer (EF Core)

## CI

GitHub Actions is used to run tests automatically on push.

## Frontend

The frontend application can be found here:  
https://github.com/Janussr/scoresync-frontend

## Notes

This project is primarily built for learning and experimentation with:
- real-time communication (SignalR)
- backend architecture
- database design
