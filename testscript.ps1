$name = read-host "enter your name"
write-host "hi, $name" -f darkgreen
for($i = 1; $i -lt 6; $i++) {
	write-host $i
	start-sleep -m 300
}
exit 3