# See https://stackoverflow.com/questions/18641864/git-bash-shell-fails-to-create-symbolic-links

# Check for Windows OS
windows() { [[ -n "$WINDIR" ]]; }

# Create symlink, cross-platform.
# With one parameter, it will check whether the parameter is a symlink.
# With two parameters, it will create a symlink to a file or directory,
# with syntax: link $linkname $target
link() {
    if [[ -z "$2" ]]; then
        # Link-checking mode.
        if windows; then
            fsutil reparsepoint query "$1" > /dev/null
        else
            [[ -h "$1" ]]
        fi
    else
        # Link-creation mode.
        if windows; then
            # Windows needs to be told if it's a directory or not. Infer that.
            # Also: note that we convert `/` to `\`. In this case it's necessary.
            if [[ -d "$2" ]]; then
                cmd <<< "mklink /D \"$1\" \"${2//\//\\}\"" > /dev/null
            else
                cmd <<< "mklink \"$1\" \"${2//\//\\}\"" > /dev/null
            fi
        else
            ln -s "$2" "$1"
        fi
    fi
}

# Remove symlink, cross-platform.
rmlink() {
    if windows; then
        # Again, Windows needs to be told if it's a file or directory.
        if [[ -d "$1" ]]; then
            rmdir "$1";
        else
            rm "$1"
        fi
    else
        rm "$1"
    fi
}

# Create symlink from Companion App SharedRepo to main game SharedRepo
cd "../UltraStar Play Companion/Assets/Common"
link "SharedRepo" "../../../UltraStar Play/Assets/Common/SharedRepo"
