$id = [Security.Principal.WindowsIdentity]::GetCurrent()
write-host "is admin? " -nonewline
([Security.Principal.WindowsPrincipal]($id)).isinrole("Administrators").tostring().tolower()