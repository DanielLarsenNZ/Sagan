& dotnet publish --output ./release -c Release -r win-x64 .

# Sagan.exe (total-items) (max-parallel) (data-size-bytes)
& ./release/Sagan.exe 10000 5 25000