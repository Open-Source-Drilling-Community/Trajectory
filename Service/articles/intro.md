---
title: "How to use the Trajectory microservice?"
output: html_document
---

Typical Usage
===
1. Upload a new Trajectory using the `Post` web api method.
2. Call the `Get` method with the identifier of the uploaded Trajectory as argument. 
The return Json object contains the Trajectory description.
3. Optionally send a `Delete` request with the identifier of the Trajectory in order to delete the Trajectory if you do not 
want to keep the Trajectory uploaded on the microservice.


