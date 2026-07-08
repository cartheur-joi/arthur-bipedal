function sysCall_init()
    jointName = 'r_elbow_y'
    joint = sim.getObject('./' .. jointName)
    if joint == -1 then
        error('Could not find joint named ' .. jointName .. ' under this model/script')
    end

    startPos = sim.getJointPosition(joint)
    targetA = startPos + math.rad(10)
    targetB = startPos - math.rad(10)
    holdTime = 1.0
    phaseStart = sim.getSimulationTime()

    sim.addLog(sim.verbosity_scriptinfos, 'Joint cycle ready for ' .. jointName)
end

function sysCall_actuation()
    local t = sim.getSimulationTime() - phaseStart

    if t < holdTime then
        sim.setJointTargetPosition(joint, startPos)
    elseif t < holdTime * 2 then
        sim.setJointTargetPosition(joint, targetA)
    elseif t < holdTime * 3 then
        sim.setJointTargetPosition(joint, targetB)
    elseif t < holdTime * 4 then
        sim.setJointTargetPosition(joint, startPos)
    else
        phaseStart = sim.getSimulationTime()
    end
end
