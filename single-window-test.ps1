# Quick Single Window Test
Write-Host "Testing single window fix..." -ForegroundColor Green
& dotnet "src\CsharpDialog.WPF\bin\Debug\net9.0-windows\CsharpDialog.WPF.dll" --title "Window Count Test" --message "You should see ONLY this dialog window.`n`nNo background windows!" --button1 "Only One Window!" --button2 "Still See Two?"
Write-Host "Test complete. Exit code: $LASTEXITCODE" -ForegroundColor Gray
