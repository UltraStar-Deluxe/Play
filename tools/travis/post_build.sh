#! /bin/sh

#winname="${UNITYCI_PROJECT_NAME}_win.tar.gz"
osxname="${UNITYCI_PROJECT_NAME}_osx.tar.gz"

# set file permissions so builds can run out of the box
for infile in $(find $(pwd)/Build | grep -E '\.exe$|\.dll$'); do
	chmod 755 $infile
done

# tar and zip the build folders
#tar -C "$(pwd)/Build" -czvf $winname "windows"
tar -C "$(pwd)/Build" -czvf $osxname "osx"

# upload the tarballs to the server for storage
#scp -i "${UPLOAD_KEYPATH}" \
#	-o stricthostkeychecking=no \
#	-o loglevel=quiet \
#	$winname "${UPLOAD_USER}@${UPLOAD_HOST}:${UPLOAD_PATH}" > /dev/null 2>&1
#scp -i "${UPLOAD_KEYPATH}" \
#	-o stricthostkeychecking=no \
#	-o loglevel=quiet \
#	$osxname "${UPLOAD_USER}@${UPLOAD_HOST}:${UPLOAD_PATH}" > /dev/null 2>&1
