### Gameplay
- the player can use as many loop as they want each level
- each loop is limited to certain number of turns each level
- when the player runs out of turn, dies or chooses to, the loop ends
- the player can choose to delete the most recently created loop
- the player can restart the current loop or restart the whole level
- the level ends when the player reaches an exit or complete certain objectives
- for each turn the player can choose one action of the 3: move, wait, interact, and ending a loop
- 
### Technical
- each level has data that can be loaded, stored in json format when saved with level editor
- a turn has 3 phases: turn start, action, results, turn end
- - player input their action during the action phase
- - the result phase is where interactions or certain objects will react accordingly to the player's actions in the previous phase
- current object/tiles ideas:
- - wall: blocks player, objects and projectiles
- - box: can be pushed, breaks after being shot 
- - laser: kills the player when the beam is touched, can be blocked with other objects
- - turret: if the player or any object moves into or inside their range, shoot at them. kills player in one shot, break boxes in 2 shots, player can hide behind boxes to block
- - crevice: a tile with lower altitude than normal, can get the player stuck. a box can be pushed into a crevice and walked over
- - trap: kills player and objects that moves over it
- - prism/mirror: redirects laser
- - conveyor: moves objects on top of it
- - door: block players, can be opened by interacting or lever in some cases
- - lever: moves conveyors, open doors, rotate prism/mirror, etc.

### Optional Expansions
- in-game level editor
- level data encrypting and sharing