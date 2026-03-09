# Strapi v5 Content Model Specification

## Lifecycle Guidance System

This document defines the **Strapi v5 content models** required to
support the **Lifecycle → Lifecycle Stage → Lifecycle Stage Task**
structure used in the "What to do when" lifecycle guidance.

The model is designed to support:

-   Reusable tasks across multiple lifecycle stages
-   Structured lifecycle navigation
-   Role and track filtering
-   Ordered task placement within stages
-   Optional stage-specific overrides for guidance and outputs

The model follows this pattern:

Lifecycle └─ Lifecycle Stage └─ Stage Task Placement └─ Task

Vocabulary entities support filtering and tagging:

Track\
Role\
Output

------------------------------------------------------------------------

# Data Model Overview

Lifecycle 1 ──────── N Lifecycle Stage

Lifecycle Stage 1 ──────── N Stage Task Placement

Task 1 ──────── N Stage Task Placement

Task N ──────── N Role

Task N ──────── 1 Track

Task N ──────── N Output

------------------------------------------------------------------------

# Collection Types

------------------------------------------------------------------------

# 1. Lifecycle

**API ID:** lifecycle\
**Display Name:** Lifecycle\
**Draft & Publish:** Enabled

Represents a top‑level lifecycle experience.

Fields: - title (Text, required) - slug (UID from title, required) -
summary (Rich text) - ownerLabel (Text) - audienceLabel (Text) -
lastUpdatedLabel (Text) - defaultView (Enumeration: card, list) -
lucidResources (Component repeatable: shared.linkCard)

Relations: - stages → one‑to‑many lifecycle-stage

------------------------------------------------------------------------

# 2. Lifecycle Stage

Represents stages such as Explore, Discovery, Alpha, Beta, Live.

Fields: - title (Text, required) - slug (UID) - summary (Rich text) -
durationLabel (Text) - order (Integer, required) - colourHex (Text) -
tagLabel (Text) - isCollapsedByDefault (Boolean)

Relations: - lifecycle → many‑to‑one lifecycle - activeTracks →
many‑to‑many track - placements → one‑to‑many stage-task-placement

------------------------------------------------------------------------

# 3. Task

Reusable canonical task content.

Fields: - title (Text, required) - slug (UID) - what (Rich text) -
whyItMatters (Rich text) - howSteps (Component repeatable:
task.howStep) - guidanceLinks (Component repeatable: shared.linkItem) -
notes (Rich text)

Relations: - track → many‑to‑one track - roles → many‑to‑many role -
leadRole → many‑to‑one role - outputs → many‑to‑many output - placements
→ one‑to‑many stage-task-placement

------------------------------------------------------------------------

# 4. Stage Task Placement

Places a task within a lifecycle stage.

Fields: - whenLabel (Text) - order (Integer) - stageNotes (Rich text) -
overrideGuidanceLinks (Component repeatable: shared.linkItem) -
visibility (Enumeration: published, draft, archived)

Relations: - stage → many‑to‑one lifecycle-stage - task → many‑to‑one
task - overrideOutputs → many‑to‑many output

------------------------------------------------------------------------

# Vocabulary Collections

## Track

Fields: - title - slug - description - colourHex

## Role

Fields: - title - slug - shortCode - colourHex - order

## Output

Fields: - title - slug - description - templateLink

------------------------------------------------------------------------

# Components

## shared.linkItem

-   label
-   url
-   sourceLabel
-   opensInNewTab

## shared.linkCard

-   label
-   description
-   url
-   icon
-   bgColourHex

## task.howStep

-   stepText

------------------------------------------------------------------------

# Editorial Principles

Tasks contain canonical guidance content.

Stage placements control: - ordering - when tasks occur - stage‑specific
notes
