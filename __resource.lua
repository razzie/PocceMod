resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

files { 
	"config/config.ini",
    "config/vehicles.ini",
	"config/props.ini",
	"config/weapons.ini",
	"config/trashpeds.ini",
	"config/pocce.ini",
	"config/pets.ini",
	"config/scenarios.ini",
    "MenuAPI.dll",
	"data/weaponanimations.meta",
	"data/weapons.meta"
}

data_file "WEAPON_ANIMATIONS_FILE" "data/weaponanimations.meta"
data_file "WEAPONINFO_FILE" "data/weapons.meta"

client_script 'PocceMod.Client.net.dll'
server_script 'PocceMod.Server.net.dll'
