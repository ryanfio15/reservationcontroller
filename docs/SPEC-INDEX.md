1:# ARCHITECTURE.md — Table Reservation Software (prototype build instructions)
9:# Part A — How to build it
11:## A1. Outcome
18:## A2. Fixed decisions — do not change these
34:## A3. Working rules
47:## A4. Agent configuration
71:## A5. Build phases
108:## A6. What only the human can do
119:## A7. When to stop and ask
123:## A8. Definition of done
135:## A9. Repository and development pipeline
176:# Part B — Reference specification
180:### Overview
186:#### What is being built
190:#### Goals
196:#### Non-goals
200:#### Where the design goes beyond the planning doc
212:#### Stack at a glance
216:### System architecture
257:### Backend
259:#### Runtime, framework and architecture
285:#### Authentication and identity
344:#### Authorization
351:#### Reservation ticket security
416:#### API design
444:#### Core domain logic
472:#### Cross-cutting concerns
480:#### Extension points
487:### Database
489:#### Engine choice
518:#### Logical data model
584:#### Immutability
598:#### Concurrency and correctness
661:#### Multi-tenancy
668:#### Security of stored data
681:#### Search and availability queries
687:#### Operations
694:### Frontend
696:#### Framework decision
725:#### App structure and render mode
744:#### Client-side authentication
754:#### Reservation ticket and check-in
769:#### Screen inventory and navigation
775:#### API integration
783:#### UI rules and distribution
789:### Azure Hosting and Operations
791:#### Compute
826:#### Data and state services
832:#### Identity services
837:#### Secrets and keys
850:#### Networking and edge
854:#### Reliability against the 99% goal
887:#### Securing and monitoring access to code and infrastructure
925:#### Observability
932:#### Infrastructure as Code and environments
940:#### Cost
983:### Testing strategy
1005:### Prototype milestones and path to production
1029:### Open questions
1045:#### Unverified facts
