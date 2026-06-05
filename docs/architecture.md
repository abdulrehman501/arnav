# System Architecture

> ⚠️ THIS DOCUMENT IS WRITTEN BY REHMAN IN HIS OWN WORDS (zero-AI rule).
> The headings and prompts below are only a scaffold — replace every prompt with
> your own explanation. Delete this banner when you start writing.

## 1. Overview
<!-- In 3–4 sentences: what does the system do, and who is it for? -->

## 2. High-level design
<!-- Describe the two halves (Unity client, Python server) and how they communicate.
     You can redraw the diagram from the README in your own words. -->

## 3. Unity / AR client
<!-- What does the Unity app do? Mention: AR Foundation, ARCore, the camera feed,
     how arrows are drawn, how it polls the /nav endpoint. -->

## 4. Python / sensor-fusion server
<!-- Sami's side, but describe at a high level: sensor fusion, FastAPI, the /nav endpoint. -->

## 5. The API seam
<!-- Why a clean HTTP/JSON contract lets the two halves be built independently.
     Reference docs/api_contract.md. -->

## 6. Data flow (end to end)
<!-- Walk one request through the system: phone asks /nav -> server fuses sensors ->
     returns JSON -> Unity draws the arrow. -->

## 7. Technology choices & justification
<!-- Why Unity + AR Foundation? Why ARCore? Why FastAPI? (These are great viva answers too.) -->

## 8. Limitations & future work
<!-- What's not done yet, what could be improved. -->
