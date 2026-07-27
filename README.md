[README.md](https://github.com/user-attachments/files/30423614/README.md)
# **🎯 Throw Damage (SCP: Secret Laboratory Plugin)**

**Throw Damage** is a plugin for SCP: Secret Laboratory powered by **LabAPI** that turns throwing ordinary items into deadly weapons\!  
Configure unique damage, concussion effects, and 3D sounds for **each item in the game** separately. Add a headshot mechanic with stunning effects and make the Micro HID a truly devastating projectile.

## **✨ Key Features**

* 💥 **Custom Damage:** Configure different damage values for body and head hits for any item (from a flashlight to the Micro HID).  
* 🔊 **Local 3D Sounds:** Bind your custom sounds (.ogg) to hits. The sound plays exactly at the victim's coordinates (nobody on the other side of the map will hear it).  
* 😵 **Headshot Effects:** Brief blinding (Flashbang), ringing in the ears (Deafened), and concussion (Concussed) upon a precise headshot.  
* 💬 **Custom Messages:** Send a unique Broadcast message to the victim upon a headshot.  
* 🛡️ **Friendly Fire Configuration:** Ability to enable or disable Friendly Fire from thrown items.

## **📥 Installation**

1. Download the latest plugin release: the ThrowDamagePlugin.dll file from the [**Releases**](http://docs.google.com/releases) tab.  
2. Place the file into the LabAPI plugins folder of your server:  
   %appdata%\\SCP Secret Laboratory\\LabAPI\\plugins\\\<server\_port\>\\  
3. Start the server once so the plugin generates the configuration file.  
4. Place your audio files (.ogg) into the **AudioPlayer** folder located in your EXILED directory (usually %appdata%\\SCP Secret Laboratory\\EXILED\\Configs\\Audio or similar).

## **⚙️ Configuration (Throw Damage.yml)**

The configuration allows you to flexibly set up any item. Use the custom\_items block, specifying the exact item name (ItemType).

### **Example Configuration:**

is\_enabled: true  
\# Deal damage to allies?  
damage\_allies: false  
\# How many seconds is the item dangerous after being thrown?  
danger\_time: 1.5

\# Custom settings for specific items  
custom\_items:  
  MicroHID:  
    body\_damage: 10  
    head\_damage: 100  
    body\_sound\_path: 'body\_hit'  
    head\_sound\_path: 'metalpipe'  
    flash\_duration: 3  
    apply\_concussion: true  
    headshot\_message: 'HEADSHOT\!'

### **Item Parameters Description:**

| Parameter | Description |
| :---- | :---- |
| body\_damage | Damage upon hitting the torso or legs. |
| head\_damage | Damage upon hitting exactly the head. |
| body\_sound\_path | File name (without .ogg) for the body hit sound. Leave '' to disable. |
| head\_sound\_path | File name for the headshot sound. |
| flash\_duration | Blindness (white screen) duration in seconds. 0 — disabled. |
| apply\_concussion | true/false. Adds deafness and screen blur upon a headshot. |
| headshot\_message | Text the victim will see on the screen (Broadcast). |

## **📝 Requirements**

* **SCP: Secret Laboratory**  
* [**LabAPI**](https://github.com/Northwood-Studios/LabAPI) (version 1.1.6 or higher)  
* [**AudioPlayer**](https://github.com/Antoniofo/AudioPlayer) (required to play the 3D sounds)

*Created by [Annorda](https://github.com/AlekseyMatsuk).*
