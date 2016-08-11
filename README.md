# OSRS Picker

###  The goal of OSRS Picker is to Find out which OSRS server is the best to connect to.

Screenshot: 

![ScreenShot](https://raw.githubusercontent.com/PoxyDoxy/OSRSPicker/master/Screenshot.jpg "Screenshot")

---

### How to Use
1. Visit [/bin/Release](https://github.com/PoxyDoxy/OSRSPicker/tree/master/OSRSPicker/bin/Release)
2. Download [OSRSPicker.exe](https://github.com/PoxyDoxy/OSRSPicker/raw/master/OSRSPicker/bin/Release/OSRSPicker.exe) from [OSRSPicker/bin/Release](https://github.com/PoxyDoxy/OSRSPicker/tree/master/OSRSPicker/bin/Release)
3. Download [HTMLAgilityPack.dll](https://github.com/PoxyDoxy/OSRSPicker/raw/master/OSRSPicker/bin/Release/HtmlAgilityPack.dll) from [OSRSPicker/bin/Release](https://github.com/PoxyDoxy/OSRSPicker/tree/master/OSRSPicker/bin/Release)

The two files must be in the same directory, as OSRSPinger requires HTMLAgilityPack.dll to be able to read the online server list.

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
 - This uses HTMLAgilityPack
 
### About the code
 - Written in C#
 - Built in Visual Studio 2010 (Gosh, I'm such a noob)
 - Written within the timespan of about 1.5 days

--- 

### To-Do

- Use http://www.runescape.com/slr.ws instead of HTMlAgilityPack to fetch the world list.
- Get 5GP.
- Buy GF.