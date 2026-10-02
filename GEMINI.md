# UNITY SCRIPTING ARCHITECTURE CONTRACT

You are working on a production-oriented Unity 2D game.

Your highest priority is to create code that is:

- Modular
- Fault-isolated
- Testable
- Maintainable
- Memory-conscious
- Performance-conscious
- Easy to debug
- Easy to extend
- Free from unnecessary coupling

Do NOT optimize for the shortest code or fastest implementation.

Optimize for long-term stability and clean system boundaries.

---

# 1. CORE ARCHITECTURE

Follow this architecture:

```text
                    GAME
                     │
                     ▼
              BOOTSTRAP / ROOT
              Composition Root
                     │
             Dependency Wiring
                     │
       ┌─────────────┼─────────────┐
       ▼             ▼             ▼
     CORE         GAMEPLAY     PRESENTATION
       │             │             │
       │             │             │
   Save/Input     Player/AI      UI/VFX/Audio
   Events/Scene   Combat/Items   Animation
       │             │             │
       └─────────────┼─────────────┘
                     │
                INTERFACES
                     │
                  EVENTS
                     │
                     ▼
                  DATA
                     │
              ScriptableObjects
                     │
                     ▼
               RUNTIME STATE
```

The dependency direction must remain controlled.

Never create arbitrary cross-system dependencies.

---

# 2. ABSOLUTELY NO SINGLETON ARCHITECTURE

Do NOT use:

```csharp
GameManager.Instance
AudioManager.Instance
UIManager.Instance
SaveManager.Instance
InventoryManager.Instance
```

Do not introduce Singleton patterns unless explicitly requested.

Do not replace Singletons with a hidden Service Locator.

Dependencies must be explicit.

Prefer:

```csharp
public class ExampleSystem
{
    private readonly IRequiredService service;

    public ExampleSystem(IRequiredService service)
    {
        this.service = service;
    }
}
```

For Unity MonoBehaviours, use explicit references, interfaces, factories, bootstrap wiring, or other appropriate dependency injection techniques.

---

# 3. COMPOSITION ROOT / BOOTSTRAP

The Bootstrap/Composition Root is responsible for assembling the game.

It may:

- Create systems
- Configure systems
- Connect dependencies
- Register services
- Initialize core infrastructure

It must NOT contain ordinary gameplay logic.

Do not turn Bootstrap into a God Object.

Its responsibility is:

```text
CREATE
CONFIGURE
CONNECT
INITIALIZE
```

Not:

```text
MOVE PLAYER
HANDLE COMBAT
CONTROL UI
RUN AI
SAVE INVENTORY
```

---

# 4. SINGLE RESPONSIBILITY

Every script must have a clear responsibility.

Bad:

```text
PlayerManager
- Movement
- Combat
- Health
- Inventory
- Animation
- Audio
- UI
- Saving
```

Prefer:

```text
PlayerMovement
PlayerCombat
PlayerHealth
PlayerInventory
PlayerAnimation
```

Before creating a script, ask:

> "What single responsibility does this script own?"

If the answer contains "and", reconsider the design.

Do not create giant scripts.

Do not create God classes.

---

# 5. COMPOSITION OVER INHERITANCE

Prefer composing behaviours from independent components.

Example:

```text
Player
├── PlayerMovement
├── PlayerCombat
├── PlayerHealth
├── PlayerInteraction
└── PlayerAnimation
```

Avoid unnecessary inheritance chains such as:

```text
Character
→ Player
→ Warrior
→ FireWarrior
→ BossFireWarrior
```

Use inheritance only when there is a genuine "is-a" relationship and it provides clear value.

---

# 6. INTERFACES DEFINE BOUNDARIES

When one system only needs a capability, depend on an interface instead of a concrete implementation.

Example:

```csharp
public interface IDamageable
{
    void TakeDamage(int amount);
}
```

Then:

```text
Weapon
   ↓
IDamageable
   ↓
 ┌───────────┬──────────┐
 ↓           ↓          ↓
Enemy       Boss      Barrel
```

The weapon must not need to know the concrete target type.

Use interfaces when they reduce coupling or make testing/replacement easier.

Do NOT create pointless interfaces for every class.

---

# 7. EVENTS / OBSERVER PATTERN

Use events for notifications between loosely coupled systems.

Example:

```text
PlayerHealth
      │
      │ HealthChanged
      ▼
    EVENT
   /  |   \
  ↓   ↓    ↓
 UI  VFX  Audio
```

The publisher should not directly control the listeners.

Do not do this:

```csharp
playerHealth.hud.UpdateHealth();
playerHealth.audio.PlayDamageSound();
playerHealth.vfx.PlayHitEffect();
```

Prefer:

```text
PlayerHealth
      ↓
HealthChanged
      ↓
listeners react independently
```

Events should communicate meaningful state changes.

Do not create events for every trivial operation.

Always unsubscribe from events appropriately.

Avoid event memory leaks.

---

# 8. DATA VS RUNTIME STATE

Keep static/shared configuration separate from runtime state.

Use ScriptableObjects for reusable configuration such as:

```text
EnemyData
WeaponData
ItemData
CharacterData
DialogueData
LevelData
```

Example:

```text
EnemyData
├── MaxHealth
├── MovementSpeed
├── Damage
├── DetectionRange
└── AttackCooldown
```

Do NOT store temporary runtime state inside shared ScriptableObjects.

Do not store:

```text
CurrentHealth
CurrentPosition
CurrentState
CurrentTarget
CurrentInventory
```

inside shared configuration assets.

Runtime state belongs to runtime objects.

Avoid duplicating large data unnecessarily.

---

# 9. INPUT ABSTRACTION

Separate input acquisition from gameplay logic.

Prefer:

```text
Keyboard
Gamepad
Touch
AI
Replay
   ↓
Input abstraction
   ↓
Player gameplay
```

Player movement should not become tightly coupled to one specific input source.

Do not mix complex input handling with movement/combat logic unless there is a clear reason.

---

# 10. STATE MACHINES

Use the State Pattern / State Machine when behaviour has multiple meaningful states.

Example:

```text
EnemyBrain
├── Idle
├── Patrol
├── Chase
├── Attack
├── Search
└── Dead
```

Avoid huge nested conditional chains.

Do NOT create a state machine for trivial behaviour.

Only introduce it when it makes behaviour easier to understand, test, or extend.

---

# 11. STRATEGY PATTERN

Use Strategy when behaviour needs to be interchangeable.

Example:

```text
IAttackStrategy
├── MeleeAttack
├── RangedAttack
└── MagicAttack
```

The owner should depend on the strategy abstraction rather than hardcoding every possible behaviour.

Do not use Strategy merely because it is a design pattern.

Use it when behaviour genuinely needs to vary.

---

# 12. SAVE SYSTEM

Gameplay must not directly depend on the storage implementation.

Prefer:

```text
Gameplay
   ↓
ISaveable / Save abstraction
   ↓
Save Service
   ↓
Storage implementation
```

The gameplay system should not care whether the data is stored as:

- JSON
- Binary
- Cloud
- Encrypted data
- Local files

Keep persistence concerns separated from gameplay logic.

---

# 13. UI MUST NOT OWN GAMEPLAY LOGIC

UI is presentation.

Bad:

```text
Button
 ↓
Directly manipulate 15 gameplay systems
```

Prefer:

```text
Gameplay
   ↓
State / Event
   ↓
UI reacts
```

The UI may request an action through an appropriate interface/command, but it should not become the authority over game rules.

Gameplay must be able to function independently of presentation wherever practical.

---

# 14. AUDIO / VFX / ANIMATION

Treat these as presentation systems.

Gameplay should communicate meaningful events/state changes.

Example:

```text
Enemy Dies
    ↓
EnemyDeath event
    ├── UI
    ├── Audio
    ├── VFX
    └── Animation
```

Do not make gameplay code directly depend on specific VFX or audio GameObjects unless there is a genuine local dependency.

If audio/VFX is unavailable, core gameplay should continue wherever possible.

---

# 15. FAULT ISOLATION

Design every system so that a failure affects the smallest possible boundary.

Example:

```text
Animation ❌
        ↓
Movement     ✅
Combat       ✅
Health       ✅
AI           ✅
```

Another example:

```text
HUD ❌
        ↓
Player      ✅
Combat      ✅
Inventory   ✅
Enemies     ✅
```

Avoid architectures where:

```text
One missing UI reference
        ↓
Entire gameplay system crashes
```

Use validation and defensive programming at system boundaries.

Do not silently swallow important exceptions.

Failures should be detectable and diagnosable.

---

# 16. NO HIDDEN DEPENDENCIES

Avoid excessive use of:

```csharp
FindObjectOfType
FindFirstObjectByType
GameObject.Find
transform.Find
Resources.FindObjectsOfTypeAll
```

especially inside Update loops or frequently executed code.

Prefer:

- Explicit serialized references
- Constructor injection where appropriate
- Bootstrap wiring
- Interfaces
- Cached references
- Factory-created dependencies

If a lookup is genuinely required, perform it deliberately and cache the result.

---

# 17. PERFORMANCE RULES

Do not optimize blindly.

First make the architecture correct, then profile.

Avoid unnecessary:

```text
Update()
LateUpdate()
FixedUpdate()
Instantiate()
Destroy()
GetComponent()
Find()
LINQ allocations
Garbage allocations
Physics queries
```

inside high-frequency paths.

Prefer:

```text
Event-driven logic
Cached references
Object pooling where justified
Appropriate update frequencies
Spatial/visibility filtering
Efficient collections
```

Do not introduce optimization complexity unless there is a measurable reason.

For example:

```text
10 enemies
```

does not require an elaborate AI scheduler.

```text
500+ active enemies
```

may justify one.

---

# 18. OBJECT POOLING

Use pooling for frequently created/destroyed objects such as:

- Projectiles
- Hit effects
- Damage numbers
- Repeated enemies
- Particles
- Temporary gameplay objects

Do not blindly pool everything.

For objects created once or very rarely, normal lifecycle management may be simpler and preferable.

---

# 19. RUNTIME DATA OWNERSHIP

Every piece of runtime data must have a clear owner.

Before creating a field, determine:

> "Who owns this state?"

Example:

```text
PlayerHealth
    owns → CurrentHealth

PlayerInventory
    owns → CurrentItems

EnemyBrain
    owns → CurrentState

GameSession
    owns → CurrentGameProgress
```

Avoid duplicate sources of truth.

Do not have:

```text
Player
 └── health = 50

UI
 └── health = 50

GameManager
 └── health = 50
```

Instead:

```text
PlayerHealth
      │
      ▼
  CurrentHealth
      │
      └── events → UI
```

One authoritative source.

---

# 20. NULL / INVALID STATE HANDLING

Handle expected invalid states deliberately.

Validate required references.

Use clear errors such as:

```csharp
Debug.LogError(...)
```

when a required dependency is missing.

Do not hide failures with empty catch blocks:

```csharp
catch { }
```

Never silently ignore a failure that could corrupt gameplay.

---

# 21. TESTABILITY

Every major gameplay system should be testable independently.

Examples:

```text
PlayerHealthTests
CombatTests
InventoryTests
EnemyAITests
SaveSystemTests
DialogueTests
```

Test:

```text
Normal case
Boundary case
Invalid input
Missing dependency
Repeated operation
Failure/recovery
```

Do not wait until the entire game is finished before testing.

---

# 22. SCRIPT CREATION PROCESS

Whenever you are asked to create or modify a script, follow this process:

```text
1. Understand the feature
        ↓
2. Identify its responsibility
        ↓
3. Identify dependencies
        ↓
4. Check existing interfaces
        ↓
5. Check existing events
        ↓
6. Check existing data structures
        ↓
7. Decide whether the feature belongs to
   Core / Gameplay / Presentation / Data
        ↓
8. Implement the smallest appropriate component
        ↓
9. Keep dependencies explicit
        ↓
10. Add validation
        ↓
11. Consider test cases
        ↓
12. Check performance implications
        ↓
13. Check for circular dependencies
        ↓
14. Check for duplicate sources of truth
        ↓
15. Compile and test
```

---

# 23. BEFORE CREATING A NEW SCRIPT

You MUST first inspect the existing project structure and relevant scripts.

Do not create a duplicate system because you failed to find an existing one.

Before creating:

```text
GameManager
InputManager
SaveManager
AudioManager
InventoryManager
EventManager
```

ask:

> "Does this system already exist in another form?"

Prefer extending an existing appropriate system over creating a duplicate.

---

# 24. BEFORE MODIFYING EXISTING CODE

Understand:

```text
Who calls this?
What calls this?
What depends on this?
What data does this own?
What events does this publish?
What events does this subscribe to?
```

Do not make a local fix that creates a global architectural problem.

If a change requires modifying several systems, explain the dependency chain before making the change.

---

# 25. ARCHITECTURAL RED FLAGS

Stop and reconsider if you are about to create:

```text
❌ Singleton
❌ God Manager
❌ 1000+ line MonoBehaviour
❌ Circular dependency
❌ Direct UI → gameplay internals
❌ Gameplay → UI internals
❌ Shared mutable global state
❌ Duplicate source of truth
❌ Massive Update loop
❌ Excessive Find/GetComponent calls
❌ ScriptableObject containing mutable runtime state
❌ Unnecessary abstraction
❌ Unnecessary design pattern
```

---

# 26. PRIORITY ORDER

When making architectural decisions, prioritize:

```text
1. Correctness
2. Clear ownership
3. Low coupling
4. Fault isolation
5. Testability
6. Maintainability
7. Performance
8. Memory efficiency
9. Convenience
```

Do NOT sacrifice correctness and architecture simply to reduce the number of lines of code.

Do NOT prematurely optimize.

Do NOT over-engineer simple features.

---

# 27. FINAL CHECK BEFORE RETURNING CODE

Before presenting or applying any script, verify:

[ ] No unnecessary Singleton
[ ] No hidden global dependency
[ ] Single responsibility
[ ] Dependencies are explicit
[ ] No circular dependency
[ ] No duplicate source of truth
[ ] Runtime state is not stored in shared configuration assets
[ ] Events are unsubscribed correctly
[ ] Required references are validated
[ ] No unnecessary Update loop
[ ] No unnecessary allocations
[ ] No unnecessary Find/GetComponent calls
[ ] UI is not responsible for gameplay rules
[ ] Core does not depend on presentation
[ ] Appropriate interfaces are used
[ ] Appropriate patterns are used only when justified
[ ] Existing project systems were checked before creating new ones
[ ] Code compiles
[ ] Feature can be tested independently

If any item fails, reconsider the implementation before proceeding.

---

# GOLDEN RULE

Do not ask:

> "What design pattern can I use here?"

Ask:

> "What responsibility does this feature have, what does it depend on, and how can I keep that dependency isolated?"

Use patterns only when they naturally solve that problem.

The goal is not to eliminate every possible bug.

The goal is to make bugs:

- Localized
- Detectable
- Testable
- Recoverable
- Preventable from propagating

Build the project so that changing one system does not require rewriting unrelated systems.
