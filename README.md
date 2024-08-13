# Cookie Clicker - first semester assignment

During my first semester as a graduate software developer, we were tasked with building a small game in C#, using the WPF interface. 

This was written without the use of classes or modules, as we had not yet reached this subject yet in our C# course. 

I hope that by releasing this other aspiring developers can learn from some of my solutions or mistakes. The commits and documentation are in Dutch. The code isn't perfect and has some small issues, but most bugs are fixed.

## Assignment goal

This assignment was aimed at teaching us how to properly follow specific issue keys with the wishes of our "customer", as well as teaching us self-reliance and how to solve problems using Google or sites like Stackoverflow.

This repository houses the assignment I submitted, which netted me a **98.0% score** on the course.

## Preview

![Particles](https://github.com/user-attachments/assets/641ac920-93d9-4287-bff4-307a57730ed6)
![Gameplay](https://github.com/user-attachments/assets/a1afd493-406b-4050-a884-fe396478cfba)
![Quests](https://github.com/user-attachments/assets/bf017c84-b60c-42b1-a375-3cddcfd4e1e1)

## Features

- Every feature is documented, albeit in Dutch, using C#'s summary system. This will show the documentation when you highlight a custom function.
- Because there are no classes, every feature is split using #regions, both in the C# file and XAML.
- The XAML is part predefined and part built programmatically. You can see how my knowledge surrounding this improved over the commits.
- Players can mute the sound effects, as well as close the game in a "menu" panel found at the bottom of the screen.
- Some aspects of the game (such as upgrades, bought upgrades, ...) will unlock based on progression. This adds an element of visual progress for the end user.
- Clicking on the cookie (or asteroid, in this case) plays a sound effect and shows falling debris and particles.
- Clicking on the cookie (asteroid) will visually show you the income per second, ontop of the falling debris.
- Players can click on the cookie manually, or buy upgrades for automatic clicking. These all have their own unique variables.
- There are a large number of upgrades available to the player to buy, each costing more and giving more progression over time.
- Upgrades can be stacked and will show in a separate panel on the left, using a scrollviewer to avoid overlap.
- Every bought upgrade has their own background and custom made pixel art displayed in the left hand panel representing bought upgrades.
- As the player progresses, they unlock upgrade bonusses that can be bought for a lot of points. This multiplies the original upgrade.
- There is a custom message box system that informs you of of an upgrade you bought or an achievement you unlocked.
- There is an achievement system called "Quests". These can be unlocked after doing a specific action multiple times.
- Players can click the quest button in the menu panel to see which quests they have unlocked.
- Every so often, a golden cookie (asteroid) will spawn which will give the players a huge bonus (skips time by 3 hours). This plays an audio cue.
- Players can hover upgrades to see their current income. This also shows a description of the item.
- The title of the window is updated with the latest score.
