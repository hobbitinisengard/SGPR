Get-ChildItem ./*.png |
  ForEach-Object {
	$newname = $_.Name.Substring(11);
	Rename-Item -Path $_.FullName -NewName $newname
	
  }