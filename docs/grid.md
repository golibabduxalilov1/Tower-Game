# Campaign grid

The latest supplied reference replaces the previous 11×8 layout: every campaign map is now16 columns ×12 rows =192 cells. Coordinates are zero-based, from(0,0) through(15,11); storage index is `y *16 + x`.

At a512px playable-grid width each square is32px, giving a512×384px grid. The game scales square cells to the available window or touch viewport; decorative borders are outside the playable grid.

Reference cell codes describe these game states:

| Code | Meaning | Game representation |
|---|---|---|
| 0 | Empty | Ground or a free buildable platform |
| 1 | Tower | A live tower occupies the coordinate |
| 2 | Obstacle | Rock; none are authored in the campaign |
| 3 | Enemy route | Current ground path preview or authored path |
| 4 | Cannot build | Entrance, exit, rail, or another placement restriction |

Reference codes are not raw persistence enum values. Towers and current routes are dynamic state; saved maps retain named cell types. Profiles, unlocks and records remain valid. Active battles from the previous grid are rejected by the existing dimension check and shown through the recovery screen.
