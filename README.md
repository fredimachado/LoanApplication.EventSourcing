# Loans Application - Event Sourcing Sample

This project demonstrates how to implement a simple loans application using event sourcing and EventStore DB,
inspired by the [Loans Appication - Python Sample Code](https://github.com/EventStore/samples/tree/main/LoanApplication/Python) provided
by EventStore.

This sample is composed by four services:

- **Automated Applicants Service**: This service is responsible for generating random loan applications.
- **Credit Check Service**: This service is responsible for performing a credit check (random 1 to 9 scores) on the loan application.
- **Decision Engine Service**: This service is responsible for making a decision on the loan application based on the credit score.
- **Underwriting Web App**: This web application allows underwriters to check loan applications and manually approve/decline if necessary.

## TODO - Add more details...
