call npm install
call npm -g install bower
call npm -g install typings
call npm -g install gulp
echo "Installing bower"
call bower install
echo "Installing typings"
call typings install --save
echo "Running gulp"
gulp
echo "Finished portal build script"
