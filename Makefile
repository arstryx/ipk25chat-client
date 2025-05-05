
build:
	dotnet publish -c Release  -o .
	
run-ds-tcp: build
	./ipk25chat-client -t tcp -s anton5.fit.vutbr.cz -p 4567 

run-ds-udp: build
	./ipk25chat-client -t udp -s anton5.fit.vutbr.cz -p 4567 

clean:
	rm -rf ipk25chat-client
	rm -rf bin/ obj/

zip:
	zip xzakha02.zip -r src Makefile README.md ipk25-chat.csproj LICENSE CHANGELOG.md .gitignore
