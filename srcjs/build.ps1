Invoke-Expression "npm install"
Invoke-Expression "npm -g install bower"
Invoke-Expression "npm -g install typings"
Invoke-Expression "npm -g install gulp"
echo "Installing bower"
Invoke-Expression "bower install"
echo "Installing typings"
Invoke-Expression "typings update --save"
echo "Installing gulp"
Invoke-Expression gulp
echo "Finished portal build script"

$lastexitcode
