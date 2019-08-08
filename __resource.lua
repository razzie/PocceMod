resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

files { 
	"config/config.ini",
	"config/config.custom.ini",
	"config/vehicles.ini",
	"config/vehicles.custom.ini",
	"config/props.ini",
	"config/props.custom.ini",
	"config/weapons.ini",
	"config/weapons.custom.ini",
	"config/trashpeds.ini",
	"config/trashpeds.custom.ini",
	"config/pocce.ini",
	"config/pets.ini",
	"config/pets.custom.ini",
	"config/scenarios.ini",
	"config/scenarios.custom.ini",
	"config/horns.ini",
	"config/horns.custom.ini",
	"MenuAPI.dll",
	"data/weaponanimations.meta",
	"data/weapons.meta"
}

data_file "WEAPON_ANIMATIONS_FILE" "data/weaponanimations.meta"
data_file "WEAPONINFO_FILE" "data/weapons.meta"

client_script 'PocceMod.Client.net.dll'
server_script 'PocceMod.Server.net.dll'
