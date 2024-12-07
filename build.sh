mkdir /app
mkdir /app/build
cp -rf ./Assets/ /app/build/assets
cp -rf start.sh /app
cd Moonlight
dotnet build Moonlight.csproj -c Release -o /host-panel/build
