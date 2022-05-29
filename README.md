# Complete Information
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/CompleteInformation/CI-Core/master/LICENSE.txt)

![Visualization](images/diagram.svg)

## Package Structure

```mermaid
graph RL;
    C[Core]
    BBW[Base Backend Web]
    BFW[Base Frontend Web]
    SA[[Server Api]]
    SBW[[Server Backend Web]]
    SFW[[Server Frontend Web]]
    PFW([Plugin Frontend Web])
    PBW([Plugin Backend Web])

    SA --> C
    SFW --> SA
    SBW --> SA
    BBW --> C
    BFW --> C
    SBW --> BBW
    PFW --> BFW
    PBW --> BBW
```
Packages with no release on nuget and only an release as part of an application, have an additional line left and right of the box.
Packages which are not part of this repo but rather part of the plugin repos are round.
