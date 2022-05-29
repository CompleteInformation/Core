# Complete Information
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/CompleteInformation/CI-Core/master/LICENSE.txt)

![Visualization](images/diagram.svg)

## Package Structure

```mermaid
graph RL;
    SC[Shared Core]
    SBW[Shared Backend Web]
    SFW[Shared Frontend Web]
    LBW[Lib Backend Web]
    LFW[Lib Frontend Web]
    BA[[Base Api]]
    BBW[[Base Backend Web]]
    BFW[[Base Frontend Web]]
    PFW([Plugin Frontend Web])
    PBW([Plugin Backend Web])

    BA --> SC
    SBW --> SC
    BBW --> BA
    BFW --> BA
    BBW --> SBW
    LBW --> SBW
    LFW --> SFW
    BFW --> SFW
    PFW --> LFW
    PBW --> LBW
```
Packages with no release on nuget and only an release as part of an application, have an additional line left and right of the box.
Packages which are not part of this repo but rather part of the plugin repos are round.
