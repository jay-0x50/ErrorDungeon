.PHONY: test server deploy deploy-web health backup

test:
	./deploy/scripts/test.sh

server:
	./deploy/scripts/run-local.sh

deploy:
	sudo ./deploy/scripts/deploy.sh

deploy-web:
	sudo ./deploy/scripts/install-webgl.sh

health:
	./deploy/scripts/health-check.sh

backup:
	sudo ./deploy/scripts/backup.sh
