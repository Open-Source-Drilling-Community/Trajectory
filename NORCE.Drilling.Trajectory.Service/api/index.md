---
title: "Project: Microservice for trajectories"
output: html_document
---

Objective
===
This project contains the microservice that allows to access the management of trajectories.


Principles
===
The microservice provides a web API and is containerized using Docker. The web api endpoint is `Trajectory/api/Trajectories`. It is possible to upload new trajectory 
(`Post` method), to modify already uploaded trajectory (`Put` method), to delete uploaded trajectories (`Delete` method). By calling the `Get` without arguments, 
it is possible to obtain a list of all the identifiers of the trajectories that have been uploaded. It is also possible 
to call the `Get`method with the ID of an uploaded trajectory.


