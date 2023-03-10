<?php

//This is the username and password sent from the client to the server for validation
$user = htmlspecialchars($_POST["user"],ENT_QUOTES); 
$password = $_POST["password"];

//Add code here to connect to your mySql database and check if the username & password credentials are correct for login.
//You can also add extra values like group id and if the user is a moderator.
//If the credentials are incorrect, change the Response result to 0.

echo '{"Response":2,"Message":"Success, you have signed in.","Username":"'.$user.'","GroupID":0,"IsModerator":false}';

?>