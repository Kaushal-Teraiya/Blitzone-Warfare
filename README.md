# 🎯 Blitzone Warfare

## Multiplayer FPS Capture The Flag

Blitzone Warfare is a multiplayer FPS Capture The Flag project built in Unity and C#, focused on exploring **multiplayer gameplay programming, networking architecture, player systems, weapon systems, AI bots, and dedicated-server gameplay**.

The project was developed as a practical exploration of building a networked multiplayer game rather than relying entirely on pre-existing gameplay solutions.

![Blitzone Warfare](CTF.png)

---

## 🎥 Gameplay Demo

### Blitzone Warfare

https://youtu.be/9sCAUyiXATg?si=M80l7BT4tphuSBNR

---

## 🎮 Download & Play

Download the playable **Blitzone Warfare** build from Google Drive:

https://drive.google.com/drive/u/2/folders/1j2Sqw39ly1grwNKql2kMVwXob89cl2SA


## 🌐 Portfolio

Full project breakdown, technical details, screenshots, and development information:

https://kaushal-portfolio-liart.vercel.app/Projects/MultiplayerCTF

---

# Project Information

| | |
|---|---|
| **Role** | Solo Developer |
| **Engine** | Unity |
| **Language** | C# |
| **Networking** | Mirror |
| **Transport** | Telepathy |
| **Platform** | PC |
| **Game Mode** | 4v4 Capture The Flag |
| **Architecture** | Dedicated Server |
| **Backend** | Firebase |
| **Status** | Playable |

---

# Overview

Blitzone Warfare is a multiplayer FPS built around a team-based Capture The Flag game mode.

The project focuses heavily on the engineering required to make gameplay systems work across a network.

The game includes:

- Multiplayer lobby
- Character selection
- Ready system
- Team assignment
- Dedicated server
- Player synchronization
- Networked shooting
- Damage
- Death and respawn
- Capture The Flag
- Player statistics
- Scoreboard
- AI bots
- Weapon systems
- Player abilities
- UI synchronization

The project evolved from a traditional host/client implementation into a **dedicated-server architecture using Mirror Networking**.

---

# Why I Made It

I wanted to understand what actually goes into building a multiplayer game beyond simply making two players appear in the same scene.

Multiplayer introduces an entirely different set of problems.

Things that work perfectly in a single-player game suddenly require answers to questions such as:

- Who is authoritative over this state?
- Where should this calculation happen?
- What happens if the client and server disagree?
- How do players synchronize their state?
- How should damage be validated?
- How should player objects survive scene changes?
- How do we handle respawning without breaking network identity?
- How do gameplay systems communicate across clients and the server?

Blitzone Warfare became my main sandbox for learning these problems through implementation.

---

# Dedicated Server Architecture

The project uses a **dedicated server architecture** rather than relying on a host player acting as both server and client.

The general flow is:

~~~text
Client
   |
   | Connect
   ↓
Dedicated Server
   |
   +---- Lobby
   |
   +---- Match
           |
           +---- Player
           +---- Player
           +---- Player
           +---- ...
~~~

The server is responsible for authoritative gameplay state while clients primarily provide input and display the resulting game state.

---

# Player Lifecycle

One of the more complex parts of the project was managing the transition from the lobby player to the gameplay player.

The current flow is:

~~~text
Character Selection
        ↓
Lobby
        ↓
NetworkRoomPlayerLobby
        ↓
Ready
        ↓
Scene Change
        ↓
NetworkMatchPlayer
        ↓
CharacterSpawner
        ↓
Gameplay Character
~~~

The `NetworkMatchPlayer` acts as the persistent network representation of the player while the actual gameplay character is spawned into the match.

This separation helped keep lobby/player-session data independent from the gameplay character itself.

---

# NetworkMatchPlayer Persistence

A significant networking issue appeared when changing scenes.

The gameplay player was being replaced during the transition, causing the `NetworkMatchPlayer` and its network identity to disappear.

The solution was to keep the `NetworkMatchPlayer` alive across the scene transition.

This allowed:

- Match player state to persist
- Network identity to remain valid
- Player connections to retain their identity
- Gameplay characters to be spawned correctly
- Player data to remain available after entering the match

This became an important part of the final dedicated-server architecture.

---

# Multiplayer Synchronization

The project uses Mirror's networking systems to synchronize gameplay state across clients.

Networked data includes things such as:

- Player names
- Teams
- Player statistics
- Ready state
- Selected character
- Player health
- Match player identity
- Gameplay state

The project also makes use of:

- `SyncVar`
- Commands
- Client RPCs
- Target RPCs
- `NetworkIdentity`
- `NetworkServer`
- `NetworkClient`

The distinction between server-side and client-side execution became an important part of the architecture.

---

# Network Identity

`NetworkIdentity` is used as the foundation for identifying and synchronizing networked objects.

The project relies on network identities for:

- Player objects
- Gameplay characters
- Match players
- Networked interactions
- Server-side player lookup

Player references can be resolved through Mirror's spawned-object collections when necessary.

For example:

~~~csharp
NetworkServer.spawned.TryGetValue(netId, out identity);
~~~

This allows gameplay objects to resolve the corresponding networked player without relying on fragile scene references.

---

# Lobby System

The multiplayer flow begins with a lobby.

Players can:

- Connect to the server
- Enter as guests
- Select a character
- Join the match
- Choose a team
- Ready up

The lobby system manages the transition from player selection into the actual gameplay scene.

---

# Character Selection

Players can select their gameplay character before entering the match.

The selected character is stored as player state and used later by the character spawning system.

This separates the player's lobby selection from the actual gameplay character instance.

---

# Character Spawning

The `CharacterSpawner` is responsible for creating the gameplay character once the match begins.

The spawning system handles:

- Team spawn points
- Available spawn positions
- Team-based spawning
- Scene loading synchronization
- Character creation

The system maintains separate spawn pools for the two teams to prevent players from spawning at arbitrary locations.

---

# Combat & Shooting

Blitzone Warfare contains a networked FPS combat system.

The project explores:

- Weapon handling
- Shooting
- Raycast-based hit detection
- Damage
- Hit body parts
- Recoil
- Weapon visuals
- Bullet trails
- Player death
- Respawn

The combat pipeline is designed around server-authoritative gameplay.

The server is responsible for validating important gameplay events rather than allowing clients to directly decide the outcome.

---

# Weapon Framework

The weapon system is separated into different responsibilities rather than being implemented as one large weapon script.

The framework includes concepts such as:

- Weapon manager
- Player weapon
- Weapon graphics
- Fire modes
- Shooting configuration
- Hit detection
- Runtime weapon state

This makes it possible to introduce different weapon behaviors without rewriting the entire player controller.

---

# Player Health

Health is synchronized through the network.

The player health system handles:

- Maximum health
- Current health
- Damage
- Healing
- Health synchronization
- Health UI

Health changes originate from server-side gameplay and are then reflected on the appropriate clients.

---

# Death & Respawn

The death system handles the transition from alive gameplay into a dead state and back into active gameplay.

The respawn system was designed to restore the **existing player character** rather than replacing the entire networked player object.

This allows the same `NetworkIdentity` to remain associated with the player throughout the match.

The system handles things such as:

- Death state
- Ragdoll
- Death camera
- Player UI
- Respawn position
- Health restoration
- Weapon restoration
- Player state restoration

---

# Ragdoll System

The player can transition from animated gameplay into a ragdoll state when killed.

The ragdoll system works alongside the death and respawn systems.

On respawn, the player is restored to an active gameplay state rather than requiring a completely new networked player object.

---

# Capture The Flag

The primary game mode is **4v4 Capture The Flag**.

Players must:

1. Protect their own flag.
2. Enter the opposing team's territory.
3. Capture the enemy flag.
4. Return it to their team's capture point.

The flag system handles networked state and player interaction with the flag.

The project also includes team-based gameplay logic to ensure the appropriate interactions occur between players and objectives.

---

# Team System

Players are assigned to teams and their team information is synchronized across the network.

Team state is used by systems such as:

- Spawn points
- Flags
- Player names
- UI
- Gameplay rules
- Score tracking
- AI bots

---

# AI Bots

The game includes AI-controlled players so matches can be populated without requiring a full set of human players.

Bots use Unity's NavMesh system for navigation and are integrated into the same gameplay environment as human players.

The bot architecture allows AI players to participate in the match while interacting with existing gameplay systems such as:

- Teams
- Weapons
- Damage
- Flags
- Objectives
- Respawning

---

# Player Statistics

Player statistics are synchronized across the network.

Tracked statistics include:

- Kills
- Deaths
- Player name
- Team
- Firebase UID

The server handles changes to important statistics.

This allows the scoreboard and other UI systems to reflect authoritative match data.

---

# Scoreboard

The game contains a synchronized scoreboard displaying player statistics during the match.

The scoreboard can display information such as:

- Player name
- Team
- Kills
- Deaths

The scoreboard is driven by the networked player state rather than maintaining an independent copy of the gameplay information.

---

# Firebase Integration

Firebase is used for backend-related player data.

The project experimented with integrating:

- Firebase Authentication
- Firebase Realtime Database
- Firestore
- Cloud Functions

Player-related data such as XP and coins can be associated with the player's Firebase identity.

This allowed the multiplayer game to experiment with persistent player data alongside the real-time multiplayer systems.

---

# Player UI

The player UI system handles both local and remote player presentation.

The system includes:

- Health display
- Health bar
- Player name
- Team-based presentation
- Local player UI
- Remote player UI

First-person UI is separated from world-space player information so local and remote players can have different presentations.

---

# Technical Challenges

## 1. Dedicated Server Migration

Migrated the project from a host/client model toward a dedicated-server architecture while maintaining the existing gameplay systems.

## 2. Scene Transition & Network Identity

Solved an issue where the gameplay transition caused the `NetworkMatchPlayer` to be destroyed, resulting in missing player identities and failed character spawning.

The solution was to persist the `NetworkMatchPlayer` across scene loads.

## 3. Player Replacement

Used Mirror's player replacement workflow to transition from the lobby representation to the match representation while preserving the player's network connection.

## 4. Respawning Existing Networked Players

Designed respawning around restoring the existing gameplay character instead of destroying and recreating the entire networked player object.

This preserves the player's `NetworkIdentity` throughout the match.

## 5. Server Authority

Separated server-authoritative gameplay from client-side presentation and input.

This required careful handling of:

- Commands
- Client RPCs
- Target RPCs
- SyncVars
- Server-side validation

## 6. Multiplayer Damage

Implemented networked damage handling while ensuring that health and death state remain authoritative.

## 7. Networked Death & Ragdoll

Integrated death, ragdoll, camera behavior, UI state, and respawn while keeping the networked player object intact.

## 8. Character Spawning

Built a team-aware character spawning system that waits for scene loading and selects appropriate spawn points.

## 9. Networked Gameplay State

Synchronized player information such as health, team, name, statistics, and match state across connected clients.

## 10. Multiplayer Debugging

A large portion of development involved diagnosing problems that only appeared when multiple clients and the dedicated server were executing the same gameplay simultaneously.

This required understanding the difference between:

- Server state
- Local client state
- Remote client state
- Network identity
- Object ownership
- RPC execution
- Scene lifecycle

---

# Architecture

The major gameplay flow can be represented as:

~~~text
                  NetworkManager
                        |
                        ↓
                  Lobby System
                        |
                        ↓
             NetworkRoomPlayerLobby
                        |
                 Character Selection
                        |
                      Ready
                        |
                   Scene Change
                        |
                        ↓
                NetworkMatchPlayer
                        |
                        ↓
                CharacterSpawner
                        |
                        ↓
                Gameplay Character
                        |
        +---------------+---------------+
        |               |               |
      Health          Combat          Player UI
        |               |               |
        +---------------+---------------+
                        |
                    Match Systems
                        |
              +---------+---------+
              |                   |
             Flag               Score
              |                   |
              +---------+---------+
                        |
                    Match State
~~~

---

# Player Architecture

The gameplay character is composed from specialized systems rather than putting every responsibility into one player controller.

Major player responsibilities are separated into systems such as:

~~~text
Player
  |
  +-- PlayerHealth
  |
  +-- PlayerDeath
  |
  +-- PlayerUIN
  |
  +-- PlayerWeaponDisplay
  |
  +-- PlayerInitialization
  |
  +-- WeaponManager
  |
  +-- PlayerShoot
  |
  +-- RagdollManager
  |
  +-- FlagHandler
~~~

This architecture was introduced to reduce the responsibilities of the original large player controller while preserving the existing gameplay behavior.

The player controller acts primarily as an orchestrator and compatibility layer between the different systems.

---

# Architecture Philosophy

The project follows several important design principles.

### Server Authority

Important gameplay state is controlled by the server rather than trusted directly from clients.

### Separation of Responsibilities

Health, death, UI, weapons, initialization, and gameplay systems are separated into dedicated components.

### Persistent Player Representation

The match player persists independently from the temporary gameplay character.

### Composition

Gameplay behavior is composed from multiple components rather than being implemented entirely inside one monolithic player class.

### Reusable Systems

Systems such as weapons, health, UI, and spawning are designed so they can be reused or extended independently.

---

# Technologies

- Unity
- C#
- Mirror Networking
- Telepathy Transport
- Unity NavMesh
- Firebase Authentication
- Firebase Realtime Database
- Firestore
- Cloud Functions
- Unity Physics
- Rigidbody
- Raycast-based Hit Detection

---

# Repository Contents

This repository contains the **C# scripts and programming systems** used to build Blitzone Warfare.

The complete Unity project is not included because the game contains a large amount of:

- 3D assets
- Animations
- VFX
- Audio
- Textures
- Builds
- Other project files

The repository is primarily intended to showcase the **gameplay programming, multiplayer networking, and C# architecture** behind the project.

---

# Project Status

**Playable**

The current version contains a functional multiplayer gameplay loop including:

- Dedicated server
- Multiplayer lobby
- Character selection
- Ready system
- Team assignment
- Gameplay scene transition
- Character spawning
- Networked shooting
- Damage
- Death
- Respawn
- Ragdoll
- Capture The Flag
- AI bots
- Scoreboard
- Player statistics

---

# Links

### 🌐 Portfolio

https://kaushal-portfolio-liart.vercel.app/Projects/MultiplayerCTF

### 🎥 Gameplay Demo

https://youtu.be/9sCAUyiXATg?si=M80l7BT4tphuSBNR

## 🎮 Download & Play

Download the playable **Blitzone Warfare** build from Google Drive:

https://drive.google.com/drive/u/2/folders/1j2Sqw39ly1grwNKql2kMVwXob89cl2SA
