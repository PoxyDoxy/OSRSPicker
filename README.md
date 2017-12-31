# OSRS Picker

###  The goal of OSRS Picker is to Find out which OSRS server is the best to connect to.

Screenshot: 

![ScreenShot](https://raw.githubusercontent.com/PoxyDoxy/OSRSPicker/master/Screenshot.jpg "Screenshot")

---

### How to Use
1. Download the latest version from [/releases](https://github.com/PoxyDoxy/OSRSPicker/releases).
2. Run "OSRSPicker".
3. Click "Scan", or if you want to take your time, click "Slow Scan". 

---
### Backstory
At the time of writing this, the Australian OldSchool RuneScape Servers had not been released, and so the game had always felt a bit on the laggy side, but over the many years of playing the same, It has become a natural feeling. 

Despite game play seeming smooth, when it came to PVP, It just didn't feel quite the same as there was noticable delay.

And so, I spent the night programming this tool, to find out which world/server would feel the best to play on.

---
#### What it does do:

  1. Grabs the latest list of OSRS Servers dynamically (http://oldschool.runescape.com/g=oldscape/slu).
  2. Pings each server with a single 32 byte ICMP packet to find its latency from your internet connection.
  3. Saves the results to a list.
  4. Sorts the list by latency, from smallest to largest (Ascending).

#### What it does NOT do:
  - Make you good at runescape.
  - Make your internet faster.

--- 

### Requirements

 - NET 4.0
 
### About the code
 - Written in C#
 - Written within the timespan of about 1.5 days
 - Makes use of Costura.Fody to merge the HTMLAgilityPack with the main exe. 
 - Originally written in VS2010, later updated with VS2015

--- 

### To-Do

- Use http://www.runescape.com/slr.ws instead of HTMlAgilityPack to fetch the world list.
- Get 5GP.
- Buy GF.