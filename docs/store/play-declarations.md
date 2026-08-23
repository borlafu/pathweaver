# Play Console declarations

Answers to transcribe into the Console. Every one follows from what the build actually does, and
the reasoning is given so a future change can be checked against it rather than guessed at.

The build these describe: no network code, no analytics SDK, no advertising SDK, no account, one
Android permission (`VIBRATE`), and two local files.

## Data safety

Play's data safety form asks what is collected and what is shared. For this build the answer is
nothing, which makes the form short but not trivial — the questions still have to be answered
deliberately.

| Question | Answer | Why |
|---|---|---|
| Does your app collect or share any of the required user data types? | **No** | There is no network code in the build. Nothing can leave the device because nothing sends anything. |
| Is all of the user data collected by your app encrypted in transit? | not applicable | Nothing is transmitted. |
| Do you provide a way for users to request that their data is deleted? | not applicable | No data is held anywhere but the device. Uninstalling removes it. |
| Does your app contain ads? | **No** | No advertising SDK is present. |
| Does your app have a privacy policy? | **Yes** | URL below. |

**If rewarded video is ever added (#58), every one of these answers changes.** An ads SDK
collects an advertising identifier, which is collected *and* shared data, and it requires a
consent flow. That is the main reason the MVP ships without one.

## Privacy policy

Play requires a policy URL for every app, including one that collects nothing.

**Published at:**

```
https://gist.github.com/borlafu/e633c5adaf432168db92969dd876ffba
```

Verified publicly readable without a login. Paste the page URL rather than the raw one: the page
renders the headings, and a reviewer opening a wall of plain text is a reviewer looking for a
reason to ask a question.

### Two copies, and which one wins

`docs/store/privacy-policy.md` in this repository is the source of truth. The gist is the
published copy.

That is a drift risk with real consequences: a policy that contradicts what the app does is a
policy violation, and two copies will not stay identical by good intentions. **Any edit goes to
the repository first, then to the gist.** If the two ever disagree, the repository is what was
reviewed and the gist is what players were shown, which is the worse half to have wrong.

Play also requires the URL to keep resolving after review. Deleting or privatising the gist
would breach that quietly, with no warning from either side.

## Content rating (IARC questionnaire)

The questionnaire is answered per category and generates ratings for every region at once.
Answer honestly; an understated rating is a policy violation, and this game has nothing to
understate.

| Question area | Answer |
|---|---|
| Category | Puzzle / casual game |
| Violence, realistic or cartoon | none |
| Blood, gore, injury | none |
| Sexuality, nudity, suggestive content | none |
| Profanity, crude humour | none |
| Controlled substances: drugs, alcohol, tobacco | none |
| Gambling, simulated or real | none. Tiles are drawn from a shuffled bag, which is randomness rather than wagering: nothing is staked and nothing is won |
| Horror, fear-inducing content | none |
| User-generated content or sharing | none |
| Users can interact or communicate | no |
| Shares user location | no |
| Allows purchases | **no** for this build. The Supporter Pass and DLC of PRD section 6 are not in the MVP |
| Digital purchases of loot boxes or randomised items | no |

The gambling answer is the one worth reading twice. A shuffled tile bag is a random draw, but
nothing is staked, no currency is spent on the outcome, and nothing of value is won. Play's
concern is wagering, not randomness.

## App content declarations

| Declaration | Answer |
|---|---|
| Target audience | 13 and over. Not directed at children, so Play's Families policy does not apply |
| Appeals to children | no |
| Ads | contains no ads |
| App access | all functionality available without restriction; no login, no gated areas |
| Government app | no |
| Financial features | none |
| Health apps | no |
| Data safety | as above |
| News app | no |
| COVID-19 contact tracing | no |

**Target audience is a real choice, not a formality.** Declaring 12 and under puts the app under
the Families policy, which brings requirements around ads, data, and content review that a
13-and-over game does not carry. The game is suitable for children, but it is not *directed* at
them, and 13+ is the honest and simpler answer.

## Store listing

Needed before the app can be reviewed. The text is here; the images are not, because they need
art that does not exist yet.

**App name** (30 characters maximum):

```
Pathweaver: Pocket Realms
```

**Short description** (80 characters maximum):

```
Cozy offline hex puzzles. Route the flow, one thumb, no timers, no interruptions.
```

**Full description** (4000 characters maximum):

```
Pathweaver: Pocket Realms is a calm route-building puzzle for one thumb and no internet.

Draw hexagonal conduits and lay them across the board to carry water, wind, crystal and trade
from their springs to the hubs that need them. Close a route early for a safe reward, or push it
further for a bigger one and risk running out of room. That choice is the whole game.

- Plays entirely offline. No connection needed, ever, and nothing is worse without one.
- Built for one hand. Everything sits within reach of a thumb.
- No timers and no reflex tests. Take as long as you like; the board waits.
- No energy limits, no lives to run out, and no advertising.
- Picks up exactly where you left it, which suits a few minutes on a commute.

Long routes earn Pivot Tokens that let you turn or retrieve a conduit you have already placed.
Short routes earn skips, so a tile you cannot use is never a dead end. Whichever way you play,
you are earning something.
```

The full description avoids claiming features the build does not have. The Daily Expedition,
Endless Wayfare, the Atlas and biomes 2 and 3 are all in the PRD and none is in the MVP, so none
is mentioned.

**Graphics still required**, and blocked on art:

| Asset | Specification |
|---|---|
| App icon | 512 x 512, 32-bit PNG, no transparency |
| Feature graphic | 1024 x 500, PNG or JPEG, no transparency |
| Phone screenshots | at least 2, ideally 4 to 8; 16:9 or 9:16, between 320 px and 3840 px on each side |

Screenshots can be captured from a device with:

```
adb exec-out screencap -p > screenshot.png
```

Worth waiting for real art. Screenshots of placeholder geometry would be an accurate
representation of an unfinished game, and the listing is the one place where that costs
downloads.
