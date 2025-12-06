--- origin_lua ---
_G.present = _G.present or {}
_G.present['new_scene'] = {point = {}, line = {}, rect = {}, circle = {}, rank = {}, description = {}, invisible = {}, unselectable = {}, link = {}}
local present = _G.present['new_scene']
present.rank["rank"] = {}
present.description["description"] = {}
present.invisible["invisible"] = {}
present.unselectable["unselectable"] = {}
present.link["link"] = {}
