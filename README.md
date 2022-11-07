<img src="/images/diff-eng-logo.png?raw=true" alt="Difference Engine Log" width="150"/> <img src="/images/AECtech_Icon-wbg25.png?raw=true" alt="AECTech Hackathon 2022" width="150"/>

# Difference Engine

This repository hosts a tool for calculating differences between BIM models as part of the AECTech 2022 Hackathon. 
Written in 180 lines of C# using .NET 6.0 and VIM API.

The Grasshopper scripts for visualizing the changes in Rhino, and the web-app for hosting the visualization can be found at     
[https://github.com/sophXmoore1/compute.rhino3d.appserver-1](https://github.com/sophXmoore1/compute.rhino3d.appserver-1).

<img src="/images/diff-eng-logo.gif?raw=true" alt="Difference Engine Demo" width="400"/> 

## Presentation 

You can [download the presentation as pdf](https://github.com/vimaec/difference-engine/blob/develop/difference-engine.pdf?raw=true) 
or [view it online](https://docs.google.com/presentation/d/e/2PACX-1vQACg-x1aFofd81DWELVLJY2yO-RP7jlrJ1bo4S-GNAuMFsRksXI2CM3l_f8fXLCX8usKlyR1CrVL-r/pub?start=false&loop=false&delayms=3000&slide=id.g14f5d6737d2_5_0). 

## How it works 

The difference engine generates JSON files containing change records and OBJ files 
representing the geometry which was added, removed, changed, resized, or moved. 


## Performance

We were able to process 10 VIM files (total 100MB), originating from 10 Revit files (total 300MB) in 5 seconds, to produce the OBJ and JSON files representing the deltas (change sets).  

## Team 

Difference Engine team:

* Augustina Aboy
* Ben Ferrer
* Christopher Diggins
* Matt Shelp
* Nick Bowker
* Nick Mundell
* Sophie Moore

## Requirements 

* Revit to VIM Exporter Plug-in [Get it for free here](https://cloud.vimaec.com)
* VIM SDK [Contact us to get a copy](https://vimaec.com/contact)

## Documentation / Source Code

[Program.cs](https://github.com/vimaec/difference-engine/blob/develop/Program.cs)
