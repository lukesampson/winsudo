$id = [Security.Principal.WindowsIdentity]::GetCurrent()
"is admin?"
([Security.Principal.WindowsPrincipal]($id)).isinrole("Administrators")