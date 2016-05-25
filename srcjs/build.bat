call npm install
call npm -g install bower
call npm -g install tsd
call npm -g install gulp
echo "Installing bower"
call bower install
echo "Installing tsd"
call tsd update --save
echo "Running gulp"
gulp
echo "Finished portal build script"
