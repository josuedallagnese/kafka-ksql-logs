#! /usr/bin/env sh

WORKSPACE=$(pwd)
GATLING_BIN_DIR="${WORKSPACE}/deps/gatling/bin"

sh ${GATLING_BIN_DIR}/gatling.sh \
  -rm local \
  -s MainSimulation \
  -rd "DESCRICAO" \
  -rf ${WORKSPACE}/user-files/results \
  -sf ${WORKSPACE}/user-files/simulations \
  -rsf ${WORKSPACE}/user-files/resources
