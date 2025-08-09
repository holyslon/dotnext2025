.PHONY: init lock clean

lock:
	terraform providers lock -platform=windows_amd64 -platform=darwin_amd64 -platform=linux_amd64 -platform=darwin_arm64 -platform=linux_arm64 -net-mirror=https://terraform-mirror.yandexcloud.net/

init: config.s3.tfbackend backend.secret.auto.tfvars.json key.json
	terraform init -backend-config=config.s3.tfbackend

key.json:
	yc iam key create --service-account-id ajee5iodog88trtj0iqi --folder-name default --output key.json

tm.key.json:
	yc iam access-key create --service-account-id ajejqk8madn7m8vpu5qh --format json > tm.key.json

config.s3.tfbackend: tm.key.json
	cat tm.key.json | jq -r  '"access_key=\"\(.access_key.key_id)\"\nsecret_key=\"\(.secret)\""' > config.s3.tfbackend

clean:
	rm config.s3.tfbackend backend.secret.auto.tfvars.json key.json tm.key.json 
