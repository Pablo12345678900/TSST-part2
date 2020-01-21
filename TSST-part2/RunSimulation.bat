start .\CableCloud\bin\Release\netcoreapp3.0\CableCloud.exe
start .\ManagerApp\bin\Release\netcoreapp3.0\ManagerApp.exe ".\Domain1.txt"
timeout 2
start .\Subnetwork\bin\Release\netcoreapp3.0\Subnetwork.exe ".\Subnetwork.txt"
timeout 1
start .\Host\bin\Release\netcoreapp3.0\Host.exe  ".\Host\bin\Release\netcoreapp3.0\DataForHost1.txt"
timeout 1
start .\Host\bin\Release\netcoreapp3.0\Host.exe  ".\Host\bin\Release\netcoreapp3.0\DataForHost2.txt"
timeout 1
start .\Node\bin\Release\netcoreapp3.0\Node.exe ".\Node\bin\Release\netcoreapp3.0\DataForRouter1.txt"
timeout 1
start .\Node\bin\Release\netcoreapp3.0\Node.exe ".\Node\bin\Release\netcoreapp3.0\DataForRouter2.txt"
timeout 1
start .\Node\bin\Release\netcoreapp3.0\Node.exe ".\Node\bin\Release\netcoreapp3.0\DataForRouter3.txt"
timeout 1
start .\Node\bin\Release\netcoreapp3.0\Node.exe ".\Node\bin\Release\netcoreapp3.0\DataForRouter4.txt"
timeout 1
start .\Node\bin\Release\netcoreapp3.0\Node.exe ".\Node\bin\Release\netcoreapp3.0\DataForRouter5.txt"
timeout 1
start .\Node\bin\Release\netcoreapp3.0\Node.exe ".\Node\bin\Release\netcoreapp3.0\DataForRouter6.txt"
timeout 1
start .\Node\bin\Release\netcoreapp3.0\Node.exe ".\Node\bin\Release\netcoreapp3.0\DataForRouter7.txt"

