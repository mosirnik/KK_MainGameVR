# KK_MainGameVR

This is a BepInEx plugin for Koikatsu that allows you to play the main game
(including the Maker) in VR. Currently only the standing (aka room-scale)
mode is supported.

This plugin is based on the [KoikatuVR](https://github.com/Ooetksh/KoikatuVR)
plugin developed by vrhth, KoikatsuVrThrowaway and Ooetksh. If you are migrating
from KoikatuVR, refer to the 'Migrating' section.

## Prerequisites

* Koikatu or Koikatsu Party
* BepInEx 5.4 or later
* SteamVR
* A VR headset supported by SteamVR
* VR controllers

## Installation

1. Make sure BepInEx 5 has been installed.
2. Download and extract the latest zip file from
  [releases](https://github.com/mosirnik/KK_MainGameVR/releases).
3. Copy two of the extracted folders into the game folder:
    * If your game is Koikatsu Party (the Steam version), copy `BepInEx` and `Koikatsu Party_Data`.
    * Otherwise, copy `BepInEx` and `Koikatu_Data`.

Now you can start Koikatsu while SteamVR is running, and it should automatically
start up in the VR mode. Alternatively, you can use the `--vr` or `--novr`
command line option to force enable or disable VR.

## Control

This plugin assumes that your VR controller has the following buttons/controls:

* Application menu button
* Trigger button
* Grip button
* Touchpad or Thumbstick

You may need to tweak button assignments in SteamVR's per-game settings if your
controllers don't natively have these. See the Controller Support section for
a list of known-to-work controllers.

In the game, each of the controllers has 3 tools: Menu, Warp and School/Hand. Only
one of them can be active at a time. You can cycle through the tools by pressing
the Application menu button. Each controller has a cyan icon indicating which
tool is currently active.

When any of the tools is active, you can press and hold the Application menu
button to see in-game help on button roles.

### Menu tool <img src="https://raw.githubusercontent.com/mosirnik/KK_MainGameVR/master/doc/img/icon_menu.png" height="30">

The menu tool comes with a little screen on which game menus, icons and texts
are shown. You can use the other controller as a virtual laser pointer, and
pull the Trigger to click on the screen. Most game interactions (specifically,
the ones that don't involve touching 3D objects) are done this way.

Pressing the Grip button while the Menu tool is active causes the screen
to be detached and left at the current position in the 3D space. Pressing it
again reclaims the screen.

A laser pointer can also generate a right click (Touchpad right), middle click
(Touchpad center) and scroll up/down (Touchpad up/down). You can also grab
a detached screen by holding Grip. Press and hold the Application menu button
while the laser is visible to see help about this.

### Warp tool <img src="https://raw.githubusercontent.com/mosirnik/KK_MainGameVR/master/doc/img/icon_warp.png" height="30">

The warp tool allows you to move around in the 3D space. 

Use the touchpad to teleport. Before you finish teleporting, you can draw a
circle along the rim of the trackpad (or similarly rotate the thumbstick)
to change your would-be orientation after teleporting.

Holding the Grip button takes you into grab action. Here you can move around
by "grabbing" the world. If you additionally press Trigger, you can also rotate
the world. Pressing both Trigger and the touchpad gives you the full power
of general 3D rotation, allowing you to turn a wall into the floor, for
example. Double click the touchpad to become upright again.

Grab action is also avaible in the school and hand tools.

### School tool <img src="https://raw.githubusercontent.com/mosirnik/KK_MainGameVR/master/doc/img/icon_school.png" height="30"> and Hand tool <img src="https://raw.githubusercontent.com/mosirnik/KK_MainGameVR/master/doc/img/icon_hand.png" height="30">

These tools are collections of Koikatsu-specific action commands and simulated
mouse/keyboard inputs. The hand tool is for H scenes, and the school tool is for
all other scenes. Other than that, these two are similar to each other. The
button mappings are configurable for each of them separately.
The default for the school tool is:

* Trigger: Walk (Roam mode)
* Grip: Grab action
* Touchpad up: F3
* Touchpad down: F1
* Touchpad left: Turn left
* Touchpad right: Turn right
* Touchpad center: Right mouse button

For the hand tool:

* Trigger: Left mouse button
* Grip: Grab action
* Touchpad up: Mouse wheel scroll up
* Touchpad down: Mouse wheel scroll down
* Touchpad left: (unassigned)
* Touchpad right: Right mouse button
* Touchpad center: Middle mouse button

For touchpad inputs, you need to press the touchpad or click the thumbstick.
Just touching the touchpad or tilting the thumbstick won't be recognized.
Exceptions to this rule are mouse wheel scroll actions and rotate actions,
which only require touching.

## Situation-specific controls

The school tool can be used when you need more complex interactions than simple
mouse clicks.

There are also a few types of context-specific controls, where you can interact
directly with 3D objects using the controllers. This type of interaction does
not require any specific tool to be selected. The tool icon disappears when
such an interaction is available.

Below is a list of situations that offer special controls.

### Roaming

In the Roaming mode, you can move around by using the school tool to walk
(default: Trigger), and turn left and right (default: Touchpad left and right).
You can also use the warp tool to teleport.

You can use the school tool to simulate ordinary mouse and keyboard
inputs, e.g. right click (default: touchpad center) for interacting with an
object.

Use the laser pointer (touchpad center) to open the middle-button menu.

You can crouch by lowering your viewpoint relative to the floor. To do this,
you can either physically move your head or use grab action to bring the
floor closer. This behavior can be disabled in the config.

### Talk scene

When talking to a character, most interactions are done through the menu.
In addition, you can touch or look at the character by putting one of the
controllers at the position you want to touch/look at, then pulling the
Trigger. 

### H scene

Caressing can be done in the same way as touching in talk scenes. Additionally,
you can switch to a different mode of caressing by pressing the Application
menu button with the controller in place.

Optionally, automatic touching can be enabled, so that you don't even need to
pull the Trigger.

You can also kiss or lick the female by moving your head to the right place.
This can be turned off in the config.

When changing location, you can use the green laser to point to a new location
icon and pull the Trigger to confirm.

## Configuration

This plugin has a lot of configuration options. It is recommended that you use
[ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager),
which allows you to change settings of this plugin from within the game.

Alternatively you can manually edit `BepInEx\config\mosirnik.kk-main-game-vr.cfg`
with a text editor.

## Controller Support

At the moment, not all VR controllers work out of the box with this plugin. Below
is an incomplete list of the current status.

If you have an experience with a controller not listed here, please comment on
<a href="https://github.com/mosirnik/KK_MainGameVR/issues/24">this issue</a>.

### Oculus Quest 2

Works out of the box.

### Valve Index

Works out of the box.

### HTC Vive

Works out of the box.

### HP motion controllers

The following button assignments are needed:

* Enumlated trackpad: (remove assignments)
* B and Y buttons: Application Menu Button
* Joystick: Trackpad position & value

In addition, you need to make it "pretend to be Oculus Touch controllers".

## Common issues

### Framerate is low

If you experience a framerate drop when the camera approaches a character,
particularly in an H scene, then the bottleneck is likely your GPU. I'd suggest
turning down the antialiasing setting using the
[GraphicsSettings](https://github.com/BepInEx/BepInEx.GraphicsSettings) plugin.
If that is not enough, consider disabling some visual effects or reducing the rendering
resolution in SteamVR.

If you experience a low framerate in school, try disabling expensive plugins or
reducing the number of students who attend the school.

## Building (for developers)

This repository uses a submodule, so be sure to update it as appropriate (e.g. by running `git submodule update --init` after you pull).

You should be able to open KK_MainGameVR.sln in Visual Studio 2019 and just hit Build to build everything.

## Using this plugin without the preload-time patcher

**Normal users don't need to do this.**

1. Delete `BepInEx\patchers\KK_MainGameVR_Patcher`.
2. Modify `Koikatu_Data\globalgamemanagers` or `Koikatsu Party_Data/globalgamemanagers`:
    1. If you already have a modified version of `globalgamemanagers` that works with a VR mod,
      you don't need to do anything. Otherwise, proceed.
    2. Rename the file to `globalgamemanagers.orig` (or whatever you want to call the backup).
    3. Open `globalgamemanagers.orig` with [UABE](https://github.com/DerPopo/UABE/releases).
    4. Select the row with the path ID "11" and the type "Build Settings", then click "Export Dump".
    5. Use a text editor to edit the generated dump file as shown below.
    6. Click "Import Dump" to load the edited dump file.
    7. Click "OK" to save the modified file as `globalgamemanagers`, so that it
      replaces the file you renamed in the first step.
    ~~~
    Before:
    0 vector enabledVRDevices
    0 Array Array (0 items)
    0 int size = 0

    After:
    0 vector enabledVRDevices
    0 Array Array (2 items)
    0 int size = 2
    [0]
     1 string data = "None"
    [1]
     1 string data = "OpenVR"
    ~~~

## Migrating

This section is for existing users of the KoikatuVR plugin.

Major differences between this plugin and Ooetksh's version of KoikatuVR include:

* It is a BepInEx 5 plugin.
* It adds a few ways of interacting with 3D objects using conrollers, like
  touching a character or changing location in H.
* It no longer reads `VRContext.xml` or `VRSettings.xml`. It uses BepInEx-style
  configuration instead.
* All keyboard shortcuts and the seated mode have been removed.

If you are migrating from KoikatuVR, make sure to remove or disable KoikatuVR
before installing this plugin.
