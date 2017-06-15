$Client = new-object Net.Sockets.TcpClient
Write-Host "Waiting For DB"

#  Attempt connection, 300 millisecond timeout, returns boolean
$Task = $Client.ConnectAsync("192.168.112.154", 3306 ).AsyncWaitHandle.WaitOne( 60000 )
     
if($Task){
    & dotnet C:\\app\Rsk.Samples.IdentityServer4.AdminUiIntegration.dll
}
else{
    Write-Host "Couldn't Connect to DB after 60 Seconds"
}