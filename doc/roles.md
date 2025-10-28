# Roles

This document describes the various roles and what abilities they grant.

`Administrator` is an ASP.NET Core Identity role.

`Season Manager` and `League Manager` are resource-based authorization roles defined by the application.

| Ability | Season Manager | League Manager | Administrator |
| :--- | :---: | :---: | :---: |
| Season: Manage Season | X | X | X |
| Season: Manage Players | X | X | X |
| Season: Manage Teams | X | X | X |
| Season: Manage Games | X | X | X |
| Season: Manage Rounds | X | X | X |
| Season: Manage Roles | View Only | X | X |
| Create Season | | X | X |
| Delete Season | | X | X |
| Edit League | | X | X |
| League Roles | | View Only | X |
| Create League | |  | X |
| Delete League | | | X |
| Create Franchise | | | X |
| Edit Franchise | | | X |
| Delete Franchise | | | X |
| Admin Area | | | X |

